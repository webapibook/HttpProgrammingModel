using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class HttpContentFacts
    {
        [Fact] 
        public async Task HttpContent_can_be_consumed_in_push_style()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://www.ietf.org/rfc/rfc2616.txt");
                response.EnsureSuccessStatusCode();
                var ms = new MemoryStream();
                await response.Content.CopyToAsync(ms);
                Assert.True(ms.Length > 0);
            }
        }

        [Fact]
        public async Task HttpContent_can_be_consumed_in_pull_style()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://www.ietf.org/rfc/rfc2616.txt");
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[2*1024];
                var len = await stream.ReadAsync(buffer, 0, buffer.Length);
                var s = Encoding.ASCII.GetString(buffer, 0, len);
                Assert.True(s.Contains("Hypertext Transfer Protocol -- HTTP/1.1"));
            }
        }

        [Fact]
        public async Task HttpContent_can_be_consumed_as_a_string()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://www.ietf.org/rfc/rfc2616.txt");
                response.EnsureSuccessStatusCode();
                var s = await response.Content.ReadAsStringAsync();
                Assert.True(s.Contains("Hypertext Transfer Protocol -- HTTP/1.1"));
            }
        }

        class GitHubUser
        {
            public string login { get; set; }
            public int id { get; set; }
            public string url { get; set; }
            public string type { get; set; }
        }

        [Fact]
        public async Task HttpContent_can_be_consumed_using_formatters()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("https://api.github.com/users/webapibook");
                response.EnsureSuccessStatusCode();
                var user = await response.Content.ReadAsAsync<GitHubUser>(new MediaTypeFormatter[]{new JsonMediaTypeFormatter()});
                Assert.Equal("webapibook", user.login);
                Assert.Equal("Organization", user.type);
            }
        }

        [Fact]
        public void StringContent_can_be_used_to_represent_plain_text()
        {
            var response = new HttpResponseMessage()
                {
                    Content = new StringContent("this is a plain text representation")
                };
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task FormUrlEncodedContent_can_be_used_to_represent_name_value_pairs()
        {
            var request = new HttpRequestMessage
                {
                    Content = new FormUrlEncodedContent(
                        new Dictionary<string, string>()
                            {
                                {"name1", "value1"},
                                {"name2", "value2"}
                            })
                };
            Assert.Equal("application/x-www-form-urlencoded",
                request.Content.Headers.ContentType.MediaType);
            var stringContent = await request.Content.ReadAsStringAsync();
            Assert.Equal("name1=value1&name2=value2", stringContent);
        }

        [Fact]
        public async Task ByteArrayContent_can_be_used_to_represent_byte_sequences()
        {
            var alreadyExistantArray = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f};
            var content = new ByteArrayContent(alreadyExistantArray);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };
            var readText = await content.ReadAsStringAsync();
            Assert.Equal("Hello", readText);
        }

        [Fact]
        public async Task StreamContent_can_be_used_when_content_is_in_a_stream()
        {
            const string thisFileName = @"..\..\HttpContentFacts.cs";
            var stream = new FileStream(thisFileName, FileMode.Open, FileAccess.Read);
            using (var content = new StreamContent(stream))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                // Assert
                var text = await content.ReadAsStringAsync();
                Assert.True(text.Contains("this string"));
            }
            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public async Task PushStreamContent_can_be_used_when_content_is_provided_by_a_stream_writer()
        {
            var xml = new XElement("root",
                                         new XElement("child1", "text"),
                                         new XElement("child2", "text")
                );
            var content = new PushStreamContent((stream, cont, ctx) =>
                {
                    using (var writer = XmlWriter.Create(stream,
                        new XmlWriterSettings { CloseOutput = true }))
                    {
                        xml.WriteTo(writer);
                    }
                });
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/xml");

            // Assert
            var text = await content.ReadAsStringAsync();
            Assert.True(text.Contains("<child1"));
        }

        [Fact]
        public async Task PushStreamContent_can_be_used_asynchronously()
        {
            const string text = "will wait for 2 seconds without blocking";
            Timer timer = null;
            var content = new PushStreamContent((stream, cont, ctx) =>
                {
                    var callback = new TimerCallback(_ =>
                        {
                            var bytes = Encoding.UTF8.GetBytes(text);
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Close();
                        });
                    timer = new Timer(callback, null,
                        TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(-1));
                });
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("text/plain");

            // Assert
            var sw = new Stopwatch();
            sw.Start();
            var receivedText = await content.ReadAsStringAsync();
            sw.Stop();
            Assert.Equal(text, receivedText);
            Assert.True(sw.ElapsedMilliseconds > 1500);
            timer.Dispose();
        }

        [Fact]
        public async Task ObjectContent_uses_media_type_formatter_to_produce_the_content()
        {
            var representation = new
                                     {
                                         field1 = "a string", 
                                         field2 = 42, 
                                         field3 = true
                                     };
            var content = new ObjectContent(
                representation.GetType(), 
                representation,
                new JsonMediaTypeFormatter());

            // Assert
            Assert.Equal("application/json",content.Headers.ContentType.MediaType);
            var text = await content.ReadAsStringAsync();
            var obj = JObject.Parse(text);
            Assert.Equal("a string", obj["field1"]);
            Assert.Equal(42, obj["field2"]);
            Assert.Equal(true, obj["field3"]);
        }
    }
}
