﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Org.BouncyCastle.Bcpg.OpenPgp
{
    class PgpSignatureTransformation : ICryptoTransform
    {
        private HashAlgorithm sig;
        private byte lastb; // Initial value anything but '\r'
        private int signatureType;
        private HashAlgorithmTag hashAlgorithm;
        private byte[] pendingWhitespace;
        private int pendingWhitespacePosition = 0;
        private bool ignoreTrailingWhitespace;

        public PgpSignatureTransformation(int signatureType, HashAlgorithmTag hashAlgorithm, bool ignoreTrailingWhitespace)
        {
            this.signatureType = signatureType;
            this.hashAlgorithm = hashAlgorithm;
            this.lastb = 0;
            this.sig = PgpUtilities.GetHashAlgorithm(hashAlgorithm);
            this.ignoreTrailingWhitespace = ignoreTrailingWhitespace;
        }

        public int SignatureType => signatureType;

        public HashAlgorithmTag HashAlgorithm => hashAlgorithm;

        bool ICryptoTransform.CanReuseTransform => true;

        bool ICryptoTransform.CanTransformMultipleBlocks => true;

        int ICryptoTransform.InputBlockSize => 1;

        int ICryptoTransform.OutputBlockSize => 1;

        private void Update(byte b)
        {
            if (signatureType == PgpSignature.CanonicalTextDocument)
            {
                doCanonicalUpdateByte(b);
            }
            else
            {
                sig.TransformBlock(new byte[] { b }, 0, 1, null, 0);
            }
        }

        private void doCanonicalUpdateByte(byte b)
        {
            if (b == '\r')
            {
                doUpdateCRLF();
            }
            else if (b == '\n')
            {
                if (lastb != '\r')
                {
                    doUpdateCRLF();
                }
            }
            else if (ignoreTrailingWhitespace && (b == ' ' || b == '\t'))
            {
                if (pendingWhitespace == null)
                {
                    pendingWhitespace = ArrayPool<byte>.Shared.Rent(128);
                }
                else if (pendingWhitespacePosition == pendingWhitespace.Length)
                {
                    var newPendingWhitespace = ArrayPool<byte>.Shared.Rent(pendingWhitespace.Length * 2);
                    pendingWhitespace.CopyTo(newPendingWhitespace, 0);
                    ArrayPool<byte>.Shared.Return(pendingWhitespace);
                    pendingWhitespace = newPendingWhitespace;
                }
                pendingWhitespace[pendingWhitespacePosition++] = b;
            }
            else
            {
                if (pendingWhitespacePosition > 0)
                {
                    sig.TransformBlock(pendingWhitespace, 0, pendingWhitespacePosition, null, 0);
                    pendingWhitespacePosition = 0;
                }
                sig.TransformBlock(new byte[] { b }, 0, 1, null, 0);
            }

            lastb = b;
        }

        private void doUpdateCRLF()
        {
            pendingWhitespacePosition = 0;
            sig.TransformBlock(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2, null, 0);
        }

        private void Update(params byte[] bytes)
        {
            Update(bytes, 0, bytes.Length);
        }

        private void Update(
            byte[] bytes,
            int off,
            int length)
        {
            if (signatureType == PgpSignature.CanonicalTextDocument)
            {
                int finish = off + length;

                for (int i = off; i != finish; i++)
                {
                    doCanonicalUpdateByte(bytes[i]);
                }
            }
            else
            {
                sig.TransformBlock(bytes, off, length, null, 0);
            }
        }

        internal void UpdateWithIdData(int header, byte[] idBytes)
        {
            this.Update(
                (byte)header,
                (byte)(idBytes.Length >> 24),
                (byte)(idBytes.Length >> 16),
                (byte)(idBytes.Length >> 8),
                (byte)(idBytes.Length));
            this.Update(idBytes);
        }

        internal void UpdateWithPublicKey(PgpPublicKey key)
        {
            byte[] keyBytes = key.publicPk.GetEncodedContents();

            this.Update(
                (byte)0x99,
                (byte)(keyBytes.Length >> 8),
                (byte)(keyBytes.Length));
            this.Update(keyBytes);
        }

        public void Finish(
            int version,
            PublicKeyAlgorithmTag keyAlgorithm,
            DateTime creationTime,
            SignatureSubpacket[] hashedSubpackets)
        {
            if (version == 3)
            {
                long time = new DateTimeOffset(creationTime, TimeSpan.Zero).ToUnixTimeSeconds();

                Update(new byte[] {
                    (byte)signatureType,
                    (byte)(time >> 24),
                    (byte)(time >> 16),
                    (byte)(time >> 8),
                    (byte)(time) });
            }
            else
            {
                Update((byte)version);
                Update((byte)this.SignatureType);
                Update((byte)keyAlgorithm);
                Update((byte)this.HashAlgorithm);

                MemoryStream hOut = new MemoryStream();

                foreach (var hashedSubpacket in hashedSubpackets)
                {
                    hashedSubpacket.Encode(hOut);
                }

                Update((byte)(hOut.Length >> 8));
                Update((byte)hOut.Length);
                Update(hOut.GetBuffer(), 0, (int)hOut.Length);

                Update((byte)version);
                Update((byte)0xff);
                int hDataLength = 4 + (int)hOut.Length + 2;
                Update((byte)(hDataLength >> 24));
                Update((byte)(hDataLength >> 16));
                Update((byte)(hDataLength >> 8));
                Update((byte)(hDataLength));
            }
        }

        public byte[] Hash => sig.Hash;

        public bool Verify(MPInteger[] sigValues, PgpPublicKey key)
        {
            byte[] signature;

            if (sigValues.Length == 1)
            {
                signature = sigValues[0].Value;
            }
            else
            {
                Debug.Assert(sigValues.Length == 2);
                int rsLength = Math.Max(sigValues[0].Value.Length, sigValues[1].Value.Length);
                signature = new byte[rsLength * 2];
                sigValues[0].Value.CopyTo(signature, rsLength - sigValues[0].Value.Length);
                sigValues[1].Value.CopyTo(signature, signature.Length - sigValues[1].Value.Length);
            }

            sig.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return key.Verify(sig.Hash, signature, hashAlgorithm);
        }

        public MPInteger[] Sign(PgpPrivateKey privateKey)
        {
            sig.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var signature = privateKey.Sign(sig.Hash, hashAlgorithm);
            MPInteger[] sigValues;
            if (privateKey.PublicKeyPacket.Algorithm == PublicKeyAlgorithmTag.RsaGeneral ||
                privateKey.PublicKeyPacket.Algorithm == PublicKeyAlgorithmTag.RsaSign)
            {
                sigValues = new MPInteger[] { new MPInteger(signature) };
            }
            else
            {
                sigValues = new MPInteger[] {
                    new MPInteger(signature.AsSpan(0, signature.Length / 2).ToArray()),
                    new MPInteger(signature.AsSpan(signature.Length / 2).ToArray())
                };
            }
            return sigValues;
        }

        int ICryptoTransform.TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Update(inputBuffer, inputOffset, inputCount);
            inputBuffer.AsSpan(inputOffset, inputCount).CopyTo(outputBuffer.AsSpan(outputOffset));
            return inputCount;
        }

        byte[] ICryptoTransform.TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            Update(inputBuffer, inputOffset, inputCount);
            return inputBuffer.AsSpan(inputOffset, inputCount).ToArray();
        }

        void IDisposable.Dispose()
        {
            if (pendingWhitespace != null)
            {
                ArrayPool<byte>.Shared.Return(pendingWhitespace);
                pendingWhitespace = null;
            }
        }
    }
}
