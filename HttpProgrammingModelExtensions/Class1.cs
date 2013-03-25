using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HttpProgrammingModelExtensions
{
    public class XmlContent : PushStreamContent
    {
        public XmlContent(XElement xe) : base(PushStream(xe), "application/xml")
        {
        }

        private static Action<Stream,HttpContent,TransportContext> PushStream(XElement xe)
        {
            return (stream, content, ctx) =>
                {
                    using (var writer = XmlWriter.Create(stream))
                    {
                        xe.WriteTo(writer);
                    }
                };
        }
    }

    public static class XElementContentExtensions
    {
        public static HttpContent ToHttpContent(this XElement xe)
        {
            return new PushStreamContent((stream, content, ctx) =>
                {
                    using (var writer = XmlWriter.Create(stream))
                    {
                        xe.WriteTo(writer);
                    }
                },"application/xml");
        }
    }
}
