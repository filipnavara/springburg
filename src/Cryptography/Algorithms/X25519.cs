﻿using Internal.Cryptography;
using NSec.Cryptography;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Springburg.Cryptography.Algorithms
{
    public class X25519 : ECDiffieHellman
    {
        Key? privateKey;
        PublicKey publicKey;

        public X25519()
        {
            this.privateKey = Key.Create(KeyAgreementAlgorithm.X25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            this.publicKey = this.privateKey.PublicKey;
        }

        public X25519(ECParameters parameters)
        {
            // TODO: Verify curve id
            if (parameters.D != null)
                this.privateKey = Key.Import(KeyAgreementAlgorithm.X25519, parameters.D, KeyBlobFormat.RawPrivateKey);
            this.publicKey = NSec.Cryptography.PublicKey.Import(KeyAgreementAlgorithm.X25519, parameters.Q.X, KeyBlobFormat.RawPublicKey);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                privateKey?.Dispose();
            }
            base.Dispose(disposing);
        }

        public override byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? secretPrepend, byte[]? secretAppend)
        {
            if (this.privateKey == null)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return ECDiffieHellmanDerivation.DeriveKeyFromHash(otherPartyPublicKey, hashAlgorithm, secretPrepend, secretAppend, DeriveSecretAgreement);
        }

        public override byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[]? hmacKey, byte[]? secretPrepend, byte[]? secretAppend)
        {
            if (this.privateKey == null)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return ECDiffieHellmanDerivation.DeriveKeyFromHmac(otherPartyPublicKey, hashAlgorithm, hmacKey, secretPrepend, secretAppend, DeriveSecretAgreement);
        }

        public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            return DeriveKeyFromHash(otherPartyPublicKey, HashAlgorithmName.SHA256, null, null);
        }

        public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
        {
            if (this.privateKey == null)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            return ECDiffieHellmanDerivation.DeriveKeyTls(otherPartyPublicKey, prfLabel, prfSeed, DeriveSecretAgreement);
        }

        private byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, System.Security.Cryptography.IncrementalHash? hash)
        {
            PublicKey otherPartyNSecPublicKey;

            if (otherPartyPublicKey is X25519PublicKey x25519PublicKey)
            {
                otherPartyNSecPublicKey = x25519PublicKey.publicKey;
            }
            else
            {
                var parameters = otherPartyPublicKey.ExportParameters();
                otherPartyNSecPublicKey = NSec.Cryptography.PublicKey.Import(KeyAgreementAlgorithm.X25519, parameters.Q.X, KeyBlobFormat.RawPublicKey);
            }
            
            Debug.Assert(this.privateKey != null);
            Debug.Assert(hash != null);

            using var sharedSecret = KeyAgreementAlgorithm.X25519.Agree(this.privateKey, otherPartyNSecPublicKey);

            // NSec doesn't offer a way to export the shared secret. Unfortunately it also doesn't provide
            // the correct key derivation function so we have to resort to using the private API.
            var memoryHandle = (SafeHandle?)typeof(SharedSecret).GetProperty("Handle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetMethod?.Invoke(sharedSecret, null);
            Debug.Assert(memoryHandle != null);
            byte[] secretBytes = new byte[32];
            try
            {
                Marshal.Copy(memoryHandle.DangerousGetHandle(), secretBytes, 0, 32);
                hash.AppendData(secretBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretBytes);
            }

            return null;
        }

        public override ECParameters ExportParameters(bool includePrivateParameters)
        {
            if (this.privateKey == null && includePrivateParameters)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);

            return new ECParameters
            {
                Curve = ECCurve.CreateFromOid(new Oid("1.3.6.1.4.1.3029.1.5.1")),
                D = includePrivateParameters ? privateKey!.Export(KeyBlobFormat.RawPrivateKey) : null,
                Q = new ECPoint { X = publicKey.Export(KeyBlobFormat.RawPublicKey), Y = new byte[32] }
            };
        }

        public override ECDiffieHellmanPublicKey PublicKey => new X25519PublicKey(publicKey);

        class X25519PublicKey : ECDiffieHellmanPublicKey
        {
            internal PublicKey publicKey;

            public X25519PublicKey(PublicKey publicKey)
            {
                this.publicKey = publicKey;
            }

            public override ECParameters ExportParameters()
            {
                return new ECParameters
                {
                    Curve = ECCurve.CreateFromOid(new Oid("1.3.6.1.4.1.3029.1.5.1")),
                    Q = new ECPoint { X = publicKey.Export(KeyBlobFormat.RawPublicKey), Y = new byte[32] }
                };
            }
        }
    }
}
