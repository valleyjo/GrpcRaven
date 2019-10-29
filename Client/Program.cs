namespace GrpcRaven
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Protobuf;
    using Grpc.Core;

    class Program
    {
        public class ClientImpl
        {
            private readonly Consumer.ConsumerClient consumeClient;
            private readonly HostProxy.HostProxyClient proxyClient;

            public ClientImpl(Channel channel)
            {
                this.consumeClient = new Consumer.ConsumerClient(channel);
                this.proxyClient = new HostProxy.HostProxyClient(channel);
            }

            public async Task<RavenResponse> ConsumeOne(CancellationToken token)
            {
                var req = new RavenMessageProto()
                {
                    Key = Guid.NewGuid().ToString(),
                    Payload = ByteString.CopyFromUtf8("Smudgie the cat"),
                };

                var options = new CallOptions(cancellationToken: token);
                return await this.consumeClient.ConsumeAsync(req, options);
            }

            public async Task<GoalStateResponse> UpdateGoalStateAsync(CancellationToken token) {
                var req = new GoalStateRequest() { Code = 0, Body = Guid.NewGuid().ToString(), };
                var options = new CallOptions(cancellationToken: token);
                return await this.proxyClient.UpdateGoalStateAsync(req, options);
            }

            public async Task<string> ConsumeTwoAsync(CancellationToken token) {
                RavenResponse r1 = await this.ConsumeOne(token);
                RavenResponse r2 = await this.ConsumeOne(token);
                // does not handle falted tasks
                // return a string so it's easy rather than a tuple or custom object
                return
                    "================================================================================\n" +
                    FormatResponse(r1) + '\n' +
                    FormatResponse(r2) + '\n' +
                    "================================================================================\n";
            }

            public async Task ConsumeMany(CancellationToken token)
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        foreach (RavenMessageProto request in this.GenerateEndpointRequests(token))
                        {
                            this.Log($"Sending message {request.Key} with {request.Payload.ToStringUtf8()}");
                            var options = new CallOptions(cancellationToken: token);
                            RavenResponse response = await this.consumeClient.ConsumeAsync(request, options);
                            this.Log($"Recieved response: code='{response.Code}' detail='{response.Detail}'");
                        }
                    }
                }
                catch (RpcException e)
                {
                    this.Log($"RPC failed: {e}");
                }
            }

            private string FormatResponse(RavenResponse resp) =>
                FormatResponse(resp.GetType().Name, resp.Code.ToString(), resp.Detail);

            private string FormatResponse(GoalStateResponse resp) =>
                FormatResponse(resp.GetType().Name, resp.Code.ToString(), resp.Detail);

            private string FormatResponse(string type, string code, string detail) =>
                $"{type}: [code='{code}' detail='{detail}']";

            private IEnumerable<RavenMessageProto> GenerateEndpointRequests(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    Task.Delay(1000, token).Wait();

                    yield return new RavenMessageProto()
                    {
                        Key = Guid.NewGuid().ToString(),
                        Payload = ByteString.CopyFromUtf8("Smudgie the cat says meow"),
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
            using (var cts = new CancellationTokenSource())
            {
                Console.WriteLine("Press ctrl+c to exit.");
                Console.CancelKeyPress += (o, e) => { cts.Cancel(); };
                //Task t = ConsumeOne(cts.Token);
                Task t = ConsumeTwoAsync(cts.Token);
                //Task t = ConsumeMany(cts.Token);
                t.Wait(cts.Token);
            }
        }

        private static async Task ConsumeMany(CancellationToken token)
        {
            const int start = 10000;
            const int end = 10010;
            var tasks = new List<Task>();
            var clients = new List<ClientImpl>();
            var channels = new List<Channel>();

            Console.WriteLine($"Starting {nameof(ConsumeMany)} scenario");

            for (int i = start; i < end; i++)
            {
                var channel = new Channel("localhost", i, ChannelCredentials.Insecure);
                var client = new ClientImpl(channel);
                channels.Add(channel);
                clients.Add(client);
            }

            foreach (ClientImpl client in clients)
            {
                tasks.Add(client.ConsumeMany(token));
            }

            await Task.Delay(-1, token);

            var shutdownTasks = new List<Task>();
            foreach (Channel c in channels)
            {
                shutdownTasks.Add(c.ShutdownAsync());
            }

            await Task.WhenAll(shutdownTasks);
        }

        private static async Task ConsumeOneAsync(CancellationToken token)
        {
            Console.Write($"Starting {nameof(ConsumeOneAsync)} scenario");
            var channel = new Channel("localhost", 10000, ChannelCredentials.Insecure);
            var client = new ClientImpl(channel);
            RavenResponse r = await client.ConsumeOne(token);
            Console.WriteLine($"Recieved response: code='{r.Code}' detail='{r.Detail}'");
        }

        private static async Task ConsumeTwoAsync(CancellationToken token)
        {
            Console.Write($"Starting {nameof(ConsumeTwoAsync)} scenario");
            var channel = new Channel("localhost", 10000, ChannelCredentials.Insecure);
            var client = new ClientImpl(channel);
            Console.WriteLine(await client.ConsumeTwoAsync(token));
        }
    }
}
