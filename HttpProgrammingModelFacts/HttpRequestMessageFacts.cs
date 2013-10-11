using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class HttpRequestMessageFacts
    {
        // {{{easy_to_instantiate
        [Fact]
        public void HttpRequestMessage_is_easy_to_instantiate()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                new Uri("http://www.ietf.org/rfc/rfc2616.txt"));

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "http://www.ietf.org/rfc/rfc2616.txt", 
                request.RequestUri.ToString());
            Assert.Equal(new Version(1,1), request.Version);
        }
        // }}}

        // {{{New_HTTP_methods_can_be_used
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
        // }}}

        [Fact]
        public void Has_setter_and_getters_for_most_properties()
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri("http://www.ietf.org/rfc/rfc2616.txt");
            request.Version = new Version(1,0);
            request.Content = new StringContent("I'm a representation");
        }

        // {{{HttpRequestMessage_has_a_CreateResponse_extension_method
        [Fact]
        public void HttpRequestMessage_has_a_CreateResponse_extension_method()
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.net"));
            var response = request.CreateResponse(HttpStatusCode.OK);
            Assert.Equal(request, response.RequestMessage);
        }
        // }}}

        // {{{CreateResponse_can_receive_a_formatter
        public void CreateResponse_can_receive_a_formatter()
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.net"));

            var response = request.CreateResponse(
                HttpStatusCode.OK,
                new { String = "hello", AnInt = 42 },
                new JsonMediaTypeFormatter());

            Assert.Equal("application/json",
                response.Content.Headers.ContentType.MediaType);
        }
        // }}}

        // {{{CreateResponse_performs_content_negotiation
        [Fact]
        public void CreateResponse_performs_content_negotiation()
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.net"));
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json", 1.0));

            var response = request.CreateResponse(
                HttpStatusCode.OK,
                "resource representation",
                new HttpConfiguration());

            Assert.Equal("application/json",
                response.Content.Headers.ContentType.MediaType);
        }
        // }}}
    }
}
