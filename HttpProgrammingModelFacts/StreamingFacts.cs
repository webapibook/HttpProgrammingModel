using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class StreamController : ApiController
    {
        public HttpResponseMessage Get(bool push)
        {
            var bytes = new byte[] {1, 2, 3, 4, 5, 6, 7};
           
            return push ? new HttpResponseMessage()
                           {
                               Content = new PushStreamContent(
                                   (stream, response, ctx) =>
                                       {
                                           stream.Write(bytes, 0, bytes.Length);
                                           stream.Close();
                                       })
                           }
                         : new HttpResponseMessage()
                           {
                               Content = new StreamContent(new MemoryStream(bytes))
                           };
        }
    }

    public class StreamingFacts
    {
        private async Task<HttpSelfHostServer> CreateAndOpenServer(int port, bool streaming)
        {
            var config = new HttpSelfHostConfiguration(string.Format("http://localhost:{0}", port))
                             {
                                 TransferMode = streaming ? TransferMode.Streamed : TransferMode.Buffered
                             };
            config.Routes.MapHttpRoute("ApiDefault", "{controller}/{id}", new {id = RouteParameter.Optional});
            var server = new HttpSelfHostServer(config);
            await server.OpenAsync();
            return server;
        }

        [Fact]
        public async Task With_SelfHosting_TransferMode_Buffered_contents_are_always_buffered()
        {
            var server = await CreateAndOpenServer(8080, false);
            using(var client = new HttpClient())
            {
                var resp = await client.GetAsync("http://localhost:8080/stream?push=false");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(0, resp.Content.Headers.ContentEncoding.Count);
                Assert.Equal(0, resp.Headers.TransferEncoding.Count);
                resp = await client.GetAsync("http://localhost:8080/stream?push=true");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(0, resp.Content.Headers.ContentEncoding.Count);
                Assert.Equal(0, resp.Headers.TransferEncoding.Count);
            }
            await server.CloseAsync();
        }

        [Fact]
        public async Task With_SelfHosting_TransferMode_Streamed_contents_are_always_streamed()
        {
            var server = await CreateAndOpenServer(8081,true);
            using (var client = new HttpClient())
            {
                var resp = await client.GetAsync("http://localhost:8081/stream?push=false");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(1, resp.Headers.TransferEncoding.Count);
                resp = await client.GetAsync("http://localhost:8081/stream?push=true");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(1, resp.Headers.TransferEncoding.Count);
            }
            await server.CloseAsync();
        }

        
        [Fact]
        public async Task With_WebHosting_Fact()
        {
            using (var client = new HttpClient())
            {
                var resp = await client.GetAsync("http://www.example.net/api/stream?streamType=MemoryStream");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(0, resp.Headers.TransferEncoding.Count);

                resp = await client.GetAsync("http://www.example.net/api/stream?streamType=NonSeekableStream");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(1, resp.Headers.TransferEncoding.Count);

                resp = await client.GetAsync("http://www.example.net/api/stream?streamType=PushStream");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(1, resp.Headers.TransferEncoding.Count);

                resp = await client.GetAsync("http://www.example.net/api/stream?streamType=CustomStreamContent");
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                Assert.Equal(0, resp.Headers.TransferEncoding.Count);
            }
        }
    }
}
