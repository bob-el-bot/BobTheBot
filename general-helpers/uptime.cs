using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Uptime
{
    /// <summary>
    /// A simple service that listens for HTTP requests and responds with a message.
    /// </summary>
    public static class Service
    {
        /// <summary>
        /// Creates an HTTP listener and starts listening for requests.
        /// </summary>
        public static void StartHttpListener()
        {
            HttpListener listener = new();
            listener.Prefixes.Add($"http://*:{Environment.GetEnvironmentVariable("PORT")}/");
            listener.Start();
            Console.WriteLine($"Listening for HTTP requests on port {Environment.GetEnvironmentVariable("PORT")}...");

            Task.Run(async () =>
            {
                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    ProcessRequest(context);
                }
            });
        }

        /// <summary>
        /// Processes an HTTP request and sends a response.
        /// </summary>
        public static void ProcessRequest(HttpListenerContext context)
        {
            _ = context.Request;
            HttpListenerResponse response = context.Response;

            // Process the request here
            string responseString = "Bob is Alive!";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}