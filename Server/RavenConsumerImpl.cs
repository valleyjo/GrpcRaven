namespace GrpcRaven
{
    using System;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class RavenConsumerImpl : Consumer.ConsumerBase
    {
        public override Task<RavenResponse> Consume(RavenMessageProto request, ServerCallContext context)
        {
            Console.WriteLine($"Recieved request: key='{request.Key}' payload='{request.Payload.ToStringUtf8()}'");
            return Task.FromResult(new RavenResponse() { Code = 0, Detail = "Freddy the cat says 'squeak'", });
        }
    }
}
