namespace Pigeon
{
    using System;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class PigeonConsumerImpl : Bombing.BombingBase
    {
        public override Task<BombResponse> SupportRequest(BombRequest request, ServerCallContext context)
        {
            string result = string.Format("Recieved {0}: key='{1}' bombCode= '{2}' payload='{3}'",
                request.GetType().ToString(),
                request.SecurityKey,
                request.BombCode.ToString(),
                request.Payload.ToStringUtf8());

            Console.WriteLine(result);
            return Task.FromResult(
                new BombResponse()
                {
                    Code = 0,
                    Detail = $"Freddy the cat says '{request.BombCode.ToString()} fired'",
                });
        }
    }
}
