namespace GrpcRaven
{
    using Grpc.Core;
    using Microsoft.Azure.Compute.Raven.Mvp;
    using System;
    using System.Threading.Tasks;

    public class RavenConsumerImpl : RavenConsumer.RavenConsumerBase
    {
        public RavenConsumerImpl()
        {
        }

        public override Task<RavenResponse> Consume(RavenMessageProto request, ServerCallContext context)
        {
            Console.WriteLine($"Recieved request: key='{request.Key}' payload='{request.Payload.ToStringUtf8()}'");
            return Task.FromResult(new RavenResponse() { Code = 234, Detail = "Freddy the cat says 'ok'", });
        }
    }
}
