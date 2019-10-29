namespace Pigeon {
    using System;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class HomingImpl : Homing.HomingBase {
        public override Task<CarrierPigeonResponse> QuickDelivery(
            CarrierPigeonMessage request,
            ServerCallContext context) {
            Console.WriteLine($"{request.GetType().ToString()}: code='{request.Code}' detail='{request.Body}'");
            return Task.FromResult(new CarrierPigeonResponse() { Code = 0, Detail = "Smudgie the cat says 'meow'", });
        }
    }
}
