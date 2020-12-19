using System;
using System.IO;
using System.Text;

namespace Org.BouncyCastle.Bcpg.OpenPgp
{
    /// <summary>Class for producing literal data packets.</summary>
    public class PgpLiteralDataGenerator
        : IStreamGenerator
    {
        public const char Binary = PgpLiteralData.Binary;
        public const char Text = PgpLiteralData.Text;
        public const char Utf8 = PgpLiteralData.Utf8;

        /// <summary>The special name indicating a "for your eyes only" packet.</summary>
        public const string Console = PgpLiteralData.Console;

        private Stream pkOut;
        private bool oldFormat;

        /// <summary>
        /// Generates literal data objects in the old format.
        /// This is important if you need compatibility with PGP 2.6.x.
        /// </summary>
        /// <param name="oldFormat">If true, uses old format.</param>
        public PgpLiteralDataGenerator(bool oldFormat = false)
        {
            this.oldFormat = oldFormat;
        }

        private void WriteHeader(
            Stream outStr,
            char format,
            byte[] encName,
            long modificationTime)
        {
            outStr.Write(new[] {
                (byte)format,
                (byte)encName.Length });

            outStr.Write(encName);

            outStr.Write(new[] {
                (byte)(modificationTime >> 24),
                (byte)(modificationTime >> 16),
                (byte)(modificationTime >> 8),
                (byte)modificationTime });
        }

        /// <summary>
        /// <p>
        /// Open a literal data packet, returning a stream to store the data inside the packet.
        /// </p>
        /// <p>
        /// The stream created can be closed off by either calling Close()
        /// on the stream or Close() on the generator. Closing the returned
        /// stream does not close off the Stream parameter <c>outStr</c>.
        /// </p>
        /// </summary>
        /// <param name="outputStream">The stream we want the packet in.</param>
        /// <param name="format">The format we are using.</param>
        /// <param name="name">The name of the 'file'.</param>
        /// <param name="length">The length of the data we will write.</param>
        /// <param name="modificationTime">The time of last modification we want stored.</param>
        public Stream Open(
            Stream outputStream,
            char format,
            string name,
            long length,
            DateTime modificationTime)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            return Open(new PacketWriter(outputStream, oldFormat), format, name, length, modificationTime);
        }

        public Stream Open(
            PacketWriter writer,
            char format,
            string name,
            long length,
            DateTime modificationTime)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (pkOut != null)
                throw new InvalidOperationException("generator already in open state");

            // Do this first, since it might throw an exception
            long unixS = new DateTimeOffset(modificationTime, TimeSpan.Zero).ToUnixTimeSeconds();

            byte[] encName = Encoding.UTF8.GetBytes(name);

            pkOut = writer.GetPacketStream(PacketTag.LiteralData, length + 2 + encName.Length + 4);

            WriteHeader(pkOut, format, encName, unixS);

            return new WrappedGeneratorStream(this, pkOut);
        }

        /// <summary>
        /// <p>
        /// Open a literal data packet, returning a stream to store the data inside the packet,
        /// as an indefinite length stream. The stream is written out as a series of partial
        /// packets with a chunk size determined by the size of the passed in buffer.
        /// </p>
        /// <p>
        /// The stream created can be closed off by either calling Close()
        /// on the stream or Close() on the generator. Closing the returned
        /// stream does not close off the Stream parameter <c>outStr</c>.
        /// </p>
        /// <p>
        /// <b>Note</b>: if the buffer is not a power of 2 in length only the largest power of 2
        /// bytes worth of the buffer will be used.</p>
        /// </summary>
        /// <param name="outputStream">The stream we want the packet in.</param>
        /// <param name="format">The format we are using.</param>
        /// <param name="name">The name of the 'file'.</param>
        /// <param name="modificationTime">The time of last modification we want stored.</param>
        /// <param name="buffer">The buffer to use for collecting data to put into chunks.</param>
        public Stream Open(
            Stream outputStream,
            char format,
            string name,
            DateTime modificationTime,
            byte[] buffer)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            return Open(new PacketWriter(outputStream, oldFormat), format, name, modificationTime, buffer);
        }

        public Stream Open(
            PacketWriter writer,
            char format,
            string name,
            DateTime modificationTime,
            byte[] buffer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (pkOut != null)
                throw new InvalidOperationException("generator already in open state");

            // Do this first, since it might throw an exception
            long unixS = new DateTimeOffset(modificationTime, TimeSpan.Zero).ToUnixTimeSeconds();

            byte[] encName = Encoding.UTF8.GetBytes(name);

            pkOut = writer.GetPacketStream(PacketTag.LiteralData, buffer);

            WriteHeader(pkOut, format, encName, unixS);

            return new WrappedGeneratorStream(this, pkOut);
        }

        /// <summary>
        /// <p>
        /// Open a literal data packet for the passed in <c>FileInfo</c> object, returning
        /// an output stream for saving the file contents.
        /// </p>
        /// <p>
        /// The stream created can be closed off by either calling Close()
        /// on the stream or Close() on the generator. Closing the returned
        /// stream does not close off the Stream parameter <c>outStr</c>.
        /// </p>
        /// </summary>
        /// <param name="outStr">The stream we want the packet in.</param>
        /// <param name="format">The format we are using.</param>
        /// <param name="file">The <c>FileInfo</c> object containg the packet details.</param>
        public Stream Open(
            Stream outStr,
            char format,
            FileInfo file)
        {
            return Open(outStr, format, file.Name, file.Length, file.LastWriteTime);
        }

        /// <summary>
        /// Close the literal data packet - this is equivalent to calling Close()
        /// on the stream returned by the Open() method.
        /// </summary>
        void IStreamGenerator.Close()
        {
            if (pkOut != null)
            {
                pkOut.Close();
                pkOut = null;
            }
        }
    }
}
