using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace WebHost.Controllers
{
    internal class DelegatingStream : Stream
    {
        private readonly Stream _stream;

        public DelegatingStream(Stream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; } 
            set { _stream.Position = value; } 
        }
    }

    public class CustomStreamContent : HttpContent
    {
        private readonly Stream _stream;

        public CustomStreamContent(Stream stream)
        {
            _stream = stream;
        }


        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return _stream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    public class StreamController : ApiController
    {
        public HttpResponseMessage Get(string streamType)
        {
            var bytes = new byte[] {1, 2, 3, 4, 5, 6, 7};
            switch (streamType)
            {
                case "MemoryStream":
                    return new HttpResponseMessage()
                               {
                                   Content = new StreamContent(new MemoryStream(bytes))
                               };
                case "NonSeekableStream":
                    return new HttpResponseMessage()
                               {
                                   Content = new StreamContent(new DelegatingStream(new MemoryStream(bytes)))
                               };
                case "PushStream":
                    return new HttpResponseMessage()
                               {
                                   Content = new PushStreamContent(
                                       (stream, response, ctx) =>
                                           {
                                               stream.Write(bytes, 0, bytes.Length);
                                               stream.Close();
                                           })
                               };
                case "CustomStreamContent":
                    return new HttpResponseMessage()
                    {
                        Content = new CustomStreamContent(new MemoryStream(bytes))
                    };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
