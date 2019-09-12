namespace GrpcRaven
{
    using Grpc.Core;
    using Microsoft.Azure.Compute.Raven.Mvp;
    using System;

    class Program
    {
        static void Main()
        {
            const int Start = 10000;
            const int End = 10100;

            var server = new Server
            {
                Services = { RavenConsumer.BindService(new RavenConsumerImpl()) },
            };

            for (int port = Start; port < End; port++)
            {
                server.Ports.Add(new ServerPort("localhost", port, ServerCredentials.Insecure));
            }

            server.Start();

            Console.WriteLine($"GrpcRavem server listening on port {Start}");
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
