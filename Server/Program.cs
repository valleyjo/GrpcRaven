namespace GrpcRaven
{
    using System;
    using Grpc.Core;

    class Program
    {
        static void Main()
        {
            const int Start = 10000;
            const int End = 10010;

            var ravenServer = new Server
            { 
                Services =
                { 
                    Consumer.BindService(new RavenConsumerImpl()),
                    HostProxy.BindService(new HostProxyImpl()),
                },
            };

            for (int port = Start; port < End; port++)
            {
                ravenServer.Ports.Add(new ServerPort("localhost", port, ServerCredentials.Insecure));
                Console.WriteLine($"GrpcRaven server listening on port {port}");
            }

            ravenServer.Start();

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            ravenServer.ShutdownAsync().Wait();
        }
    }
}
