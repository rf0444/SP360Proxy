using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SP360Proxy
{
    public class MultiOutputStream : Stream
    {
        private IEnumerable<Stream> streams;
        private Action<IEnumerable<Tuple<Stream, Exception>>> onError;

        public MultiOutputStream(IEnumerable<Stream> streams, Action<IEnumerable<Tuple<Stream, Exception>>> onError)
        {
            this.streams = streams;
            this.onError = onError;
        }

        public override bool CanRead
        {
            get
            {
                return this.streams.All(x => x.CanRead);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.streams.All(x => x.CanSeek);
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.streams.All(x => x.CanWrite);
            }
        }

        public override long Length
        {
            get
            {
                return this.streams.Min(x => x.Length);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            foreach (var stream in this.streams)
            {
                stream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            foreach (var stream in this.streams)
            {
                stream.SetLength(value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var errors = new List<Tuple<Stream, Exception>>();
            foreach (var stream in this.streams)
            {
                try
                {
                    stream.Write(buffer, offset, count);
                }
                catch (Exception e)
                {
                    errors.Add(Tuple.Create(stream, e));
                }
            }
            if (errors.Count > 0)
            {
                this.onError(errors);
            }
        }
    }
}