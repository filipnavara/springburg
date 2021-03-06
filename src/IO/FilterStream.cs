using System.IO;

namespace Springburg.IO
{
    class FilterStream : Stream
    {
        public FilterStream(Stream s)
        {
            this.s = s;
        }

        public override bool CanRead => s.CanRead;

        public override bool CanSeek => s.CanSeek;

        public override bool CanWrite => s.CanWrite;

        public override long Length => s.Length;

        public override long Position
        {
            get { return s.Position; }
            set { s.Position = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                s.Close();
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            s.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            s.SetLength(value);
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            return s.Read(buffer, offset, count);
        }
        public override int ReadByte()
        {
            return s.ReadByte();
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            s.WriteByte(value);
        }
        
        protected readonly Stream s;
    }
}
