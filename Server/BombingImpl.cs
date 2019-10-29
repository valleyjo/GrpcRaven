namespace Pigeon
{
    using System;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class PigeonConsumerImpl : Bombing.BombingBase
    {
        public override Task<MortarResponse> FireMortar(MortarRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Recieved {request.GetType().ToString()}: key='{request.SecurityKey}' payload='{request.Payload.ToStringUtf8()}'");
            return Task.FromResult(new MortarResponse() { Code = 0, Detail = "Freddy the cat says 'mortars fired'", });
        }
    }
}
