namespace Pigeon
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
            private readonly Bombing.BombingClient bombingClient;
            private readonly Homing.HomingClient homingClient;

            public ClientImpl(Channel channel)
            {
                this.bombingClient = new Bombing.BombingClient(channel);
                this.homingClient = new Homing.HomingClient(channel);
            }

            public async Task<BombResponse> ConsumeOne(CancellationToken token)
            {
                var req = new BombRequest()
                {
                    X = 45,
                    Y = 50,
                    SecurityKey = Guid.NewGuid().ToString(),
                    Payload = ByteString.CopyFromUtf8("Smudgie the cat"),
                };

                var options = new CallOptions(cancellationToken: token);
                return await this.bombingClient.SupportRequestAsync(req, options);
            }

            public async Task<CarrierPigeonResponse> QuickDeliveryAsync(CancellationToken token) {
                var req = new CarrierPigeonMessage() { Code = 0, Body = Guid.NewGuid().ToString(), };
                var options = new CallOptions(cancellationToken: token);
                return await this.homingClient.QuickDeliveryAsync(req, options);
            }

            public async Task<string> ConsumeTwoAsync(CancellationToken token) {
                CarrierPigeonResponse r1 = await this.QuickDeliveryAsync(token);
                CarrierPigeonResponse r2 = await this.QuickDeliveryAsync(token);
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
                        foreach (BombRequest request in this.GenerateMortarRequest(token))
                        {
                            this.Log($"Sending message {request.SecurityKey} with {request.Payload.ToStringUtf8()}");
                            var options = new CallOptions(cancellationToken: token);
                            BombResponse response = await this.bombingClient.SupportRequestAsync(request, options);
                            this.Log(FormatResponse(response));
                        }
                    }
                }
                catch (RpcException e)
                {
                    this.Log($"RPC failed: {e}");
                }
            }

            private IEnumerable<BombRequest> GenerateMortarRequest(CancellationToken token)
            {
                var rand = new Random();
                while (!token.IsCancellationRequested)
                {
                    Task.Delay(1000, token).Wait();

                    yield return new BombRequest()
                    {
                        X = (ulong)rand.Next(100),
                        Y = (ulong)rand.Next(100),
                        SecurityKey = Guid.NewGuid().ToString(),
                        BombCode = BombCode.Mortar,
                        Payload = ByteString.CopyFromUtf8("Smudgie the cat says fire the mortar."),
                    };
                }
            }

            private void Log(string s)
            {
                Console.WriteLine(s);
            }
        }

        static void Main(string[] args)
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
            BombResponse r = await client.ConsumeOne(token);
            Console.WriteLine($"Recieved response: code='{r.Code}' detail='{r.Detail}'");
        }

        private static async Task ConsumeTwoAsync(CancellationToken token)
        {
            Console.Write($"Starting {nameof(ConsumeTwoAsync)} scenario");
            var channel = new Channel("localhost", 10000, ChannelCredentials.Insecure);
            var client = new ClientImpl(channel);
            Console.WriteLine(await client.ConsumeTwoAsync(token));
        }

        private static string FormatResponse(CarrierPigeonResponse resp) =>
            FormatResponse(resp.GetType().Name, resp.Code.ToString(), resp.Detail);

        private static string FormatResponse(BombResponse resp) =>
            FormatResponse(resp.GetType().Name, resp.Code.ToString(), resp.Detail);

        private static string FormatResponse(string type, string code, string detail) =>
            $"{type}: [code='{code}' detail='{detail}']";

    }
}
