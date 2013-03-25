using System;
using System.Collections.Concurrent;
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
using System.Web.Http.SelfHost;

namespace SelfHostedProxy
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
            var server = new HttpSelfHostServer(config, new ProxyMessageHandler());
            server.OpenAsync().Wait();
            Trace.TraceInformation("server is opened");
            Console.ReadKey();
            server.CloseAsync().Wait();
            Trace.TraceInformation("server is closed, bye");
        }
    }

    internal class ProxyMessageHandler : DelegatingHandler
    {
        private static readonly HttpClient client = new HttpClient();
        async protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try{
                var host = request.Headers.Host;
                request.RequestUri = new Uri(new Uri(request.RequestUri.Scheme + "://" + host), request.RequestUri.PathAndQuery);
                request.Headers.TransferEncoding.Clear();
                RemoveConnectionHeadersFrom(request.Headers, request.Headers.Connection);
                if(request.Content != null && request.Content.Headers.ContentType == null)
                {
                    request.Content = null;
                }
                Trace.TraceInformation("forwarding {0} request to {1}", request.Method, request.RequestUri);
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.Headers.TransferEncoding.Clear();
                RemoveConnectionHeadersFrom(response.Headers, response.Headers.Connection);
                Trace.TraceInformation("received {0} response from {1} with status {2}", 
                    request.Method, request.RequestUri, response.StatusCode);
                
                return response;
            }catch(Exception e)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                           {
                                               Content = new StringContent(e.Message)
                                           };
            }
        }

        private static void RemoveConnectionHeadersFrom(HttpHeaders headers, IEnumerable<string> names )
        {
            foreach (var name in names)
            {
                headers.Remove(name);
            }
            headers.Remove("Connection");
        }
    }
}
