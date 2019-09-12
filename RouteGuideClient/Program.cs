namespace GrpcRaven
{
    using Google.Protobuf;
    using Grpc.Core;
    using Microsoft.Azure.Compute.Raven.Mvp;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        public class RavenConsumerClient
        {
            private readonly RavenConsumer.RavenConsumerClient client;

            public RavenConsumerClient(RavenConsumer.RavenConsumerClient client)
            {
                this.client = client;
            }

            public async Task<RavenResponse> ConsumeOne()
            {
                var req = new RavenMessageProto()
                {
                    Key = Guid.NewGuid().ToString(),
                    Payload = ByteString.CopyFromUtf8("Smudgie the cat"),
                };

                return await this.client.ConsumeAsync(req);
            }

            public async Task Consume(CancellationToken token)
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        foreach (RavenMessageProto request in this.GenerateEndpointRequests(token))
                        {
                            this.Log($"Sending message {request.Key} with {request.Payload.ToStringUtf8()}");
                            var options = new CallOptions(cancellationToken: token, deadline: DateTime.UtcNow + TimeSpan.FromSeconds(1));
                            RavenResponse response = await this.client.ConsumeAsync(request);
                            this.Log($"Recieved response: code='{response.Code}' detail='{response.Detail}'");
                        }
                    }
                }
                catch (RpcException e)
                {
                    this.Log($"RPC failed: {e}");
                    throw;
                }
            }

            private IEnumerable<RavenMessageProto> GenerateEndpointRequests(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    Task.Delay(100, token).Wait();

                    yield return new RavenMessageProto()
                    {
                        Key = Guid.NewGuid().ToString(),
                        Payload = ByteString.CopyFromUtf8("Smudgie the cat"),
                    };
                }
            }

            private void Log(string s)
            {
                Console.WriteLine(s);
            }
        }

        static void Main()
        {
            //Task t = ConsumeOne();
            Task t = ConsumeMany();
            t.Wait();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task ConsumeMany()
        {
            const int start = 10000;
            const int end = 10100;
            var tasks = new List<Task>();
            var clients = new List<RavenConsumerClient>();
            var channels = new List<Channel>();
            var cts = new CancellationTokenSource();

            Console.Write($"Starting {nameof(ConsumeMany)} scenario");
            Console.CancelKeyPress += (o, e) => { cts.Cancel(); };
            Console.WriteLine("Press any key to exit...");

            for (int i = start; i < end; i++)
            {
                var channel = new Channel("localhost", i, ChannelCredentials.Insecure);
                var client = new RavenConsumerClient(new RavenConsumer.RavenConsumerClient(channel));
                channels.Add(channel);
                clients.Add(client);
            }

            foreach (RavenConsumerClient client in clients)
            {
                tasks.Add(client.Consume(cts.Token));
            }

            Console.ReadKey();
            cts.Cancel();

            await Task.WhenAll(tasks);

            var shutdownTasks = new List<Task>();
            foreach (Channel c in channels)
            {
                shutdownTasks.Add(c.ShutdownAsync());
            }

            await Task.WhenAll(shutdownTasks);
        }

        private static async Task ConsumeOne()
        {
            Console.Write($"Starting {nameof(ConsumeOne)} scenario");
            var channel = new Channel("localhost", 10000, ChannelCredentials.Insecure);
            var client = new RavenConsumerClient(new RavenConsumer.RavenConsumerClient(channel));
            RavenResponse r = await client.ConsumeOne();
            Console.WriteLine($"Recieved response: code='{r.Code}' detail='{r.Detail}'");
        }
    }
}
