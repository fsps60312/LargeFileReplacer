using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using (AnonymousPipeServerStream pipedServer = new AnonymousPipeServerStream(PipeDirection.Out))
            {

                Thread child = new Thread(new ParameterizedThreadStart(childThread));
                child.Start(pipedServer.GetClientHandleAsString());

                using (StreamWriter sw = new StreamWriter(pipedServer))
                {
                    var data = string.Empty;
                    sw.AutoFlush = true;
                    while (!data.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        pipedServer.WaitForPipeDrain();
                        Console.WriteLine("SERVER : ");
                        data = Console.ReadLine();
                        sw.WriteLine(data);
                    }


                }

            }
        }

        public static void childThread(object parentHandle)
        {
            using (AnonymousPipeClientStream pipedClient = new AnonymousPipeClientStream(PipeDirection.In, parentHandle.ToString()))
            {
                using (StreamReader reader = new StreamReader(pipedClient))
                {
                    var data = string.Empty;
                    while ((data = reader.ReadLine()) != null)
                    {
                        Console.WriteLine("CLIENT:" + data.ToString());
                    }
                    Console.Write("[CLIENT] Press Enter to continue...");
                    Console.ReadLine();
                }
            }
        }


    }
}