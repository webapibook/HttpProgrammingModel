using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class HttpRequestMessageFacts
    {
        [Fact]
        public void HttpRequestMessage_is_easy_to_instantiate()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                new Uri("http://www.ietf.org/rfc/rfc2616.txt"));

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://www.ietf.org/rfc/rfc2616.txt", request.RequestUri.ToString());
            Assert.Equal(new Version(1,1), request.Version);
        }

        [Fact]
        public async Task New_HTTP_methods_can_be_used()
        {
            var request = new HttpRequestMessage(
                new HttpMethod("PATCH"),
                new Uri("http://www.ietf.org/rfc/rfc2616.txt"));
            using(var client = new HttpClient())
            {
                var resp = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, resp.StatusCode);
            }
        }

        [Fact]
        public void Has_setter_and_getters_for_most_properties()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri("http://www.ietf.org/rfc/rfc2616.txt");
            request.Version = new Version(1,0);
            request.Content = new StringContent("I'm a representation");
        }
    }
}
