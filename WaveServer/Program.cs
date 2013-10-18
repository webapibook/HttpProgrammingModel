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
using NAudio.Wave;

namespace WaveServer
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
                new {id = RouteParameter.Optional});
            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
            Trace.TraceInformation("server is opened");
            Console.ReadKey();
            server.CloseAsync().Wait();
            Trace.TraceInformation("server is closed, bye");
        }
    }

    public class WaveController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var content = new PushStreamContent(PushWave);
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("audio/x-wav");
            return new HttpResponseMessage()
                       {
                           Content = content
                       };
        }

        private static void PushWave(Stream stream, HttpContent content, TransportContext ctx)
        {
            var counter = 0;
            var wavein = new WaveInEvent {WaveFormat = new WaveFormat()};
            wavein.StartRecording();
            wavein.DataAvailable += (sender, args) =>
                                        {
                                            Trace.TraceInformation("wave date available: {0}", counter += args.BytesRecorded);
                                            try
                                            {
                                                stream.Write(args.Buffer, 0, args.BytesRecorded);
                                            }catch(Exception)
                                            {
                                                Trace.TraceInformation("Stop recording");
                                                wavein.StopRecording();
                                            }
                                        };
        }
    }
}
