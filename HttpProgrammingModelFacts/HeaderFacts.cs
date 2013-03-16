using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class HeaderFacts
    {
        [Fact]
        public void Header_classes_expose_headers_in_a_strongly_typed_way()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add(
                "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> accept = request.Headers.Accept;
            Assert.Equal(4,accept.Count);

            MediaTypeWithQualityHeaderValue third = accept.Skip(2).First();
            Assert.Equal("application/xml", third.MediaType);
            Assert.Equal(0.9, third.Quality);
            Assert.Null(third.CharSet);
            Assert.Equal(1,third.Parameters.Count);
            Assert.Equal("q",third.Parameters.First().Name);
            Assert.Equal("0.9", third.Parameters.First().Value);
        }

        [Fact]
        public void Header_properties_simplify_header_construction()
        {
            var response = new HttpResponseMessage();
            response.Headers.Date = new DateTimeOffset(2013,1,1,0,0,0, TimeSpan.FromHours(0));
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                MaxAge = TimeSpan.FromMinutes(1),
                Private = true
            };
            
            var dateValue = response.Headers.First(h => h.Key == "Date").Value.First();
            Assert.Equal("Tue, 01 Jan 2013 00:00:00 GMT", dateValue);

            var cacheControlValue = response.Headers.First(h => h.Key == "Cache-Control").Value.First();
            Assert.Equal("max-age=60, private", cacheControlValue);
        }

        [Fact]
        public async void Message_and_content_headers_are_not_in_the_same_collection()
        {
            using(var client = new HttpClient())
            {
                var response = await client.GetAsync("http://tools.ietf.org/html/rfc2616");
                var request = response.RequestMessage;
                Assert.Equal("tools.ietf.org",request.Headers.Host);
                Assert.NotNull(response.Headers.Server);
                Assert.Equal("text/html",response.Content.Headers.ContentType.MediaType);
            }
        }

        [Fact]
        public void HttpHeader_has_an_Add_instance_method()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Accept", "text/html;q=1.0,application/json;q=0.9");
            Assert.True(request.Headers.Accept.Any(mt => mt.MediaType == "text/html" && mt.Quality == 1.0));
            Assert.True(request.Headers.Accept.Any(mt => mt.MediaType == "application/json" && mt.Quality == 0.9));
        }

        [Fact]
        public void HttpHeader_Add_method_validates_the_value_domain_for_standard_headers()
        {
            var request = new HttpRequestMessage();
            Assert.Throws<FormatException>(() => request.Headers.Add("Date", "invalid-date"));
            request.Headers.Add("Strict-Transport-Security", "invalid ;; value");
        }

        [Fact]
        public async void HttpHeader_TryAddWithoutValidation_does_not_validates_the_value_but_preserves_it()
        {
            var request = new HttpRequestMessage();
            Assert.True(request.Headers.TryAddWithoutValidation("Date", "invalid-date"));
            Assert.Equal(null, request.Headers.Date);
            Assert.Equal("invalid-date", request.Headers.First(h => h.Key == "Date").Value.First());

            var content = new HttpMessageContent(request);
            var s = await content.ReadAsStringAsync();
            Assert.True(s.Contains("Date: invalid-date"));
        }

        [Fact]
        public void HttpHeader_TryAddWithoutValidation_does_validates_the_name()
        {
            var request = new HttpRequestMessage();
            Assert.False(request.Headers.TryAddWithoutValidation("Content-Type", "text/html"));
            Assert.False(request.Headers.TryAddWithoutValidation("", "text/html"));
        }

        [Fact]
        public void HttpHeader_can_Add_multiple_values_if_the_header_supports_it()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Accept", "text/html;q=1.0");
            request.Headers.Add("Accept", "application/json;q=.9");
            Assert.Equal("text/html", request.Headers.Accept.First().MediaType);
            Assert.Equal("application/json", request.Headers.Accept.Skip(1).First().MediaType);

            Assert.Equal(1, request.Headers.Count());
            Assert.Equal("text/html; q=1.0", request.Headers.First().Value.First());
            Assert.Equal("application/json; q=.9", request.Headers.First().Value.Skip(1).First());
        }

        [Fact]
        public void HttpHeader_cannot_Add_multiple_values_if_the_header_type_does_not_supports_it()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Date", "Tue, 12 Mar 2013 21:40:00 GMT");
            Assert.Throws<FormatException>(() => request.Headers.Add("Date", "Wed, 13 Mar 2013 21:40:00 GMT"));
        }

        [Fact]
        public void Some_header_values_are_represented_by_HttpHeaderValueCollection()
        {
            var request = new HttpRequestMessage();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html", 1.0));
            request.Headers.Accept.ParseAdd("application/json;q=.9");
        }

        [Fact]
        public void Scalar_headers()
        {
            var request = new HttpRequestMessage();
            request.Headers.Date = DateTimeOffset.UtcNow;
        }

        [Fact]
        public void Collection_headers()
        {
            var request = new HttpRequestMessage();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html", 1.0));
        }

        [Fact]
        public void PseudoCollection_headers()
        {
            var response = new HttpResponseMessage();
            response.Headers.CacheControl = new CacheControlHeaderValue
                                               {
                                                   MaxAge = TimeSpan.FromMinutes(1),
                                                   Private = true
                                               };
            var s = response.Headers.CacheControl.ToString();
            Assert.Equal(1,response.Headers.First().Value.Count());
        }
    }
}
