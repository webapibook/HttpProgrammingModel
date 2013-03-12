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
    public class HttpResponseMessageFacts
    {
        [Fact]
        public void HttpResponseMessage_is_easy_to_instantiate()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(1,1), response.Version);
        }

        public void Has_setter_and_getters_for_most_properties()
        {
            var response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;
            response.ReasonPhrase = "Okay";
            response.Version = new Version(1,1);
            response.Content = new StringContent("I'm a representation");
        }
    }
}
