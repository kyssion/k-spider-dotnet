using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.tool;

[TestClass]
public class HttpTest
{
    [TestMethod]
    public void StartNewWebServer()
    {
        var task = Task.Run(() =>
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            // Create a listener.
            var listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/");
            listener.Start();
            Console.WriteLine("Listening...");
            // Note: The GetContext method blocks while waiting for a request.
            var context = listener.GetContext();
            var request = context.Request;
            // Obtain a response object.
            var response = context.Response;
            response.AddHeader("this-sfsfsf", "12345");
            // Construct a response.
            var responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
            listener.Stop();
        });
    }
}