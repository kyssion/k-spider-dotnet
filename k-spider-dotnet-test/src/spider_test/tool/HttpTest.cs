using System.Net;
using k_spider_dotnet.tool.html;

namespace k_spider_dotnet_test.spider_test.tool;

[TestClass]
public class HttpTest
{
    [TestMethod]
    public void TestHttpDownloadImg()
    {

        var item = HtmlGetImgDownLoad.DownloadImgToFilePath("","https://np-newspic.dfcfw.com/download/D25742678450563462865_w1200h900.jpg","testImg").Result;
    }

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
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/");
            listener.Start();
            Console.WriteLine("Listening...");
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            response.AddHeader("this-sfsfsf", "12345");
            // Construct a response.
            string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
            listener.Stop();
        });
    }
}