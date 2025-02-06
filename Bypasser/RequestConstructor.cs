using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bypasser
{
    public static class RequestConstructor
    {
        public static HttpRequestMessage Create(HttpMethod method, string url, Dictionary<string, string> headers)
        {
            var req = new HttpRequestMessage(method, url);

            foreach (var header in headers)
            {
                req.Headers.Add(header.Key, header.Value);
            }

            return req;
        }

        public static HttpRequestMessage Create(HttpMethod method, string url)
        {
            //var req = new HttpRequestMessage();

            //req.Method = method;

            //var uriField = typeof(HttpRequestMessage).GetField("_requestUri", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //uriField.SetValue(req, url);

            return new HttpRequestMessage(method, url);
        }
    }
}
