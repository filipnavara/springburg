﻿using Springburg.Cryptography.OpenPgp.Packet;
using Springburg.IO;
using System;
using System.IO;
using System.Text;

namespace Springburg.Cryptography.OpenPgp
{
    /// <summary>
    /// Reader for S-expression keys. This class will move when it finds a better home!
    /// </summary>
    /// <remarks>
    /// Format documented here:
    /// http://git.gnupg.org/cgi-bin/gitweb.cgi?p=gnupg.git;a=blob;f=agent/keyformat.txt;h=42c4b1f06faf1bbe71ffadc2fee0fad6bec91a97;hb=refs/heads/master
    /// http://people.csail.mit.edu/rivest/Sexp.txt
    /// </remarks>
    class SXprReader
    {
        Stream stream;
        int peekedByte;

        public SXprReader(Stream stream)
        {
            this.stream = stream;
            this.peekedByte = -1;
        }

        private int ReadByte()
        {
            if (this.peekedByte > 0)
            {
                int pb = this.peekedByte;
                this.peekedByte = 0;
                return pb;
            }
            return stream.ReadByte();
        }

        private void UnreadByte(int pb)
        {
            this.peekedByte = pb;
        }

        private int ReadLength()
        {
            int ch;
            int len = 0;

            while ((ch = ReadByte()) >= 0 && ch >= '0' && ch <= '9')
            {
                len = len * 10 + (ch - '0');
            }
            UnreadByte(ch);

            return len;
        }

        public string ReadString()
        {
            SkipWhitespace();
            
            int ch = ReadByte();
            if (ch >= '0' && ch <= '9')
            {
                UnreadByte(ch);

                int len = ReadLength();
                ch = ReadByte();
                if (ch == ':')
                {
                    char[] chars = new char[len];

                    for (int i = 0; i != chars.Length; i++)
                    {
                        chars[i] = (char)ReadByte();
                    }

                    return new string(chars);
                }
                else if (ch == '"')
                {
                    return ReadQuotedString(len);
                }
                throw new IOException("unsupported encoding");
            }
            else if (ch == '"')
            {
                return ReadQuotedString(0);
            }
            else if (ch == '{' || ch == '|' || ch == '#')
            {
                // TODO: Unsupported encoding
                throw new IOException("unsupported encoding");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                while (IsTokenChar(ch))
                {
                    sb.Append((char)ch);
                    ch = (char)ReadByte();
                }
                UnreadByte(ch);
                return sb.ToString();
            }
        }

        private string ReadQuotedString(int length)
        {
            StringBuilder sb = new StringBuilder(length);
            int ch;
            bool skipNewLine = false;
            do
            {
                ch = ReadByte();
                if ((ch == '\n' || ch == '\r') && skipNewLine)
                {
                    skipNewLine = false;
                }
                else if (ch == '\\')
                {
                    ch = (char)ReadByte();
                    switch (ch)
                    {
                        case 'b': sb.Append('\b'); break;
                        case 't': sb.Append('\t'); break;
                        case 'v': sb.Append('\v'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'f': sb.Append('\f'); break;
                        case '"': sb.Append('"'); break;
                        case '\'': sb.Append('\''); break;
                        case '\r':
                        case '\n':
                            skipNewLine = true;
                            break;
                        default:
                            // TODO: Octal value, hexadecimal value
                            throw new IOException("unsupported encoding");
                    }
                }
                else if (ch != '"' && ch >= 0)
                {
                    skipNewLine = false;
                    sb.Append((char)ch);
                }
            }
            while (ch != '"' && ch > 0);
            return sb.ToString();
        }

        private static bool IsTokenChar(int ch)
        {
            return
                (ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= '0' && ch <= '9') ||
                ch == '-' || ch == '.' ||
                ch == '/' || ch == '_' ||
                ch == ':' || ch == '*' ||
                ch == '+' || ch == '=';
        }

        public byte[] ReadBytes()
        {
            SkipWhitespace();

            int ch = ReadByte();
            if (ch >= '0' && ch <= '9')
            {
                UnreadByte(ch);

                int len = ReadLength();

                if (ReadByte() != ':')
                    throw new IOException("unsupported encoding");

                byte[] data = new byte[len];

                stream.ReadFully(data);

                return data;
            }
            else if (ch == '#')
            {
                MemoryStream bytes = new MemoryStream();
                do
                {
                    ch = ReadByte();
                    while (char.IsWhiteSpace((char)ch))
                        ch = ReadByte();
                    if (ch == '#')
                        break;
                    int digit0 = HexToNumber(ch);
                    if (digit0 < 0)
                        throw new IOException("invalid hex encoding");
                    ch = ReadByte();
                    while (char.IsWhiteSpace((char)ch))
                        ch = ReadByte();
                    int digit1 = HexToNumber(ch);
                    if (digit1 < 0)
                        throw new IOException("invalid hex encoding");
                    bytes.WriteByte((byte)((digit0 << 4) + digit1));
                }
                while (ch != '#' && ch >= 0);
                return bytes.ToArray();
            }

            throw new IOException("unsupported encoding");
        }

        private static int HexToNumber(int c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 0xa;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 0xa;
            return -1;
        }

        public S2k ParseS2k()
        {
            SkipOpenParenthesis();

            string alg = ReadString();
            byte[] iv = ReadBytes();
            long iterationCount = Int64.Parse(ReadString());

            SkipCloseParenthesis();

            // we have to return the actual iteration count provided.
            return new MyS2k(PgpHashAlgorithm.Sha1, iv, iterationCount);
        }

        public void SkipWhitespace()
        {
            int ch = ReadByte();
            while (ch == ' ' || ch == '\r' || ch == '\n')
            {
                ch = ReadByte();
            }
            UnreadByte(ch);
        }

        public void SkipOpenParenthesis()
        {
            SkipWhitespace();

            int ch = ReadByte();
            if (ch != '(')
                throw new IOException("unknown character encountered");
        }

        public void SkipCloseParenthesis()
        {
            SkipWhitespace();

            int ch = ReadByte();
            if (ch != ')')
                throw new IOException("unknown character encountered");
        }

        private class MyS2k : S2k
        {
            private readonly long mIterationCount64;

            internal MyS2k(PgpHashAlgorithm algorithm, byte[] iv, long iterationCount64)
                : base(algorithm, iv, (int)iterationCount64)
            {
                this.mIterationCount64 = iterationCount64;
            }

            public override long IterationCount
            {
                get { return mIterationCount64; }
            }
        }
    }
}
