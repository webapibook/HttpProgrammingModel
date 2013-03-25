using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace MJpegServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            var config = new HttpSelfHostConfiguration("http://localhost:8080")
            {
                TransferMode = TransferMode.Streamed
            };
            config.Routes.MapHttpRoute(
                "ApiDefault",
                "{controller}/{id}",
                new { id = RouteParameter.Optional });
            config.ReceiveTimeout = TimeSpan.FromHours(1);
            config.SendTimeout = TimeSpan.FromHours(1);
            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
            Trace.TraceInformation("server is opened");
            Console.ReadKey();
            server.CloseAsync().Wait();
            Trace.TraceInformation("server is closed, bye");
        }
    }

    public class ScreenController : ApiController
    {
        private readonly MultipartContent _content = new MultipartContent("x-mixed-replace");

        public HttpResponseMessage Get()
        {
            Trace.TraceInformation("Get");
            var content = new PushStreamContent(PushImage);
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("image/jpeg");
            _content.Add(content);
            return new HttpResponseMessage()
            {
                Content = _content
            };
        }

        private void PushImage(Stream stream, HttpContent cont, TransportContext ctx)
        {
            Trace.TraceInformation("pushing new frame");
            var content = new PushStreamContent(PushImage);
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("image/jpeg");
            _content.Add(content);
            ScreenCapturer.GetEncodedBytesInto(stream);
            Thread.Sleep(2*1000);
            stream.Close();
        }
    }
}
