using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DumbGateway
{
    class Program
    {
        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            if (args.Length != 1 || !File.Exists(Path.GetFullPath(args[0])) || Directory.Exists(Path.GetFullPath(args[0])))
            {
                Console.WriteLine(@"usage: DumbGateway.exe path\to\config.xml");
                return;
            }
            Configuration config;
            using (StreamReader reader = new StreamReader(Path.GetFullPath(args[0])))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
                config = xmlSerializer.Deserialize(reader) as Configuration;
            }
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://" + config.Host + ":" + config.Port.ToString() + "/");
            listener.Start();
            Thread listnerThread = new Thread(delegate ()
              {
                  while (true)
                  {
                      try
                      {
                          HttpListenerContext httpListenerContext = listener.GetContext();
                          Task.Factory.StartNew(delegate (object context)
                          {
                              HttpListenerRequest request = (context as HttpListenerContext).Request;
                              HttpListenerResponse response = (context as HttpListenerContext).Response;
                              response.KeepAlive = false;
                              Endpoint matchingEndpoint = config.Endpoints.Find((endpoint) => { return endpoint.Path == request.RawUrl; });
                              if (matchingEndpoint == null)
                              {
                                  response.StatusCode = 404;
                              }
                              else
                              {
                                  response.StatusCode = 200;
                                  response.ContentType = matchingEndpoint.ResponseContentType;
                                  ProcessStartInfo startInfo = new ProcessStartInfo(matchingEndpoint.FileName, matchingEndpoint.Arguments);
                                  startInfo.WorkingDirectory = string.IsNullOrWhiteSpace(matchingEndpoint.StartDirectory) ? currentDirectory : matchingEndpoint.StartDirectory;
                                  startInfo.UseShellExecute = false;
                                  startInfo.RedirectStandardOutput = true;
                                  try
                                  {
                                      using (MemoryStream outputBuffer = new MemoryStream())
                                      {
                                          using (Process process = Process.Start(startInfo))
                                          {
                                              if (matchingEndpoint.Response == Response.StandardOutput && process != null)
                                              {
                                                  process.StandardOutput.BaseStream.CopyTo(outputBuffer);
                                                  process.WaitForExit(30000);
                                                  process.StandardOutput.BaseStream.CopyTo(outputBuffer);
                                              }
                                          }
                                          if (matchingEndpoint.Response == Response.StaticFile)
                                          {
                                              using (FileStream fileStream = new FileStream(matchingEndpoint.ResponseFileName, FileMode.Open))
                                              {
                                                  fileStream.CopyTo(outputBuffer);
                                              }
                                          }
                                          response.ContentLength64 = outputBuffer.Length;
                                          outputBuffer.Position = 0;
                                          while (outputBuffer.Position < outputBuffer.Length)
                                          {
                                              response.OutputStream.WriteByte((byte)outputBuffer.ReadByte());
                                          }
                                      }
                                  }
                                  catch (Exception)
                                  {
                                      response.StatusCode = 500;
                                  }
                              }
                              response.Close();
                          }, httpListenerContext);
                      }
                      catch (Exception) { }
                  }
              });
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                if (listener.IsListening) { listener.Stop(); }
                if (listnerThread.IsAlive) { listnerThread.Abort(); }
                listener.Close();
            };
            listnerThread.IsBackground = true;
            listnerThread.Start();
            Console.WriteLine("Start listening on port " + config.Port.ToString() + ". Press Ctrl+C to stop.");
            listnerThread.Join();
            Console.WriteLine("Bye.");
        }
    }

    public class Configuration
    {
        public int Port = 80;
        public string Host = "localhost";
        public List<Endpoint> Endpoints = new List<Endpoint>();
    }

    public class Endpoint
    {
        public string Path = "/";
        public string FileName = "";
        public string Arguments = "";
        public string StartDirectory = "";
        public Response Response = Response.Blank;
        public string ResponseFileName = "";
        public string ResponseContentType = "text/plain";
    }

    public enum Response
    {
        Blank,
        StaticFile,
        StandardOutput
    }
}
