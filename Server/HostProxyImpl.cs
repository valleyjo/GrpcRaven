namespace GrpcRaven {
    using Grpc.Core;
    using GrpcRaven;
    using System;
    using System.Threading.Tasks;

    internal class HostProxyImpl : HostProxy.HostProxyBase {
        public override Task<GoalStateResponse> UpdateGoalState(GoalStateRequest request, ServerCallContext context) {
            Console.WriteLine($"Recieved request: code='{request.Code}' detail='{request.Body}'");
            return Task.FromResult(new GoalStateResponse() { Code = 0, Detail = "Smudgie the cat says 'meow'", });
        }
    }
}
