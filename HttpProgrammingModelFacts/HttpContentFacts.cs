using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HttpProgrammingModelFacts
{
    public class HttpContentFacts
    {
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
        public async void FormUrlEncodedContent_can_be_used_to_represent_name_value_pairs()
        {
            var request = new HttpRequestMessage
                              {
                                  Content = new FormUrlEncodedContent(new Dictionary<string, string>()
                                                                          {
                                                                              {"name1", "value1"},
                                                                              {"name2", "value2"}
                                                                          })
                              };
            Assert.Equal("application/x-www-form-urlencoded", request.Content.Headers.ContentType.MediaType);
            var stringContent = await request.Content.ReadAsStringAsync();
            Assert.Equal("name1=value1&name2=value2", stringContent);
        }
    }
}
