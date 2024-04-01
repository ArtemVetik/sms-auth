using Amazon.Runtime.Endpoints;

namespace Agava.SmsAuthServer
{
    internal class EndpointProvider : IEndpointProvider
    {
        public Endpoint ResolveEndpoint(EndpointParameters parameters)
        {
            return new Endpoint(Environment.GetEnvironmentVariable("DocumentEndpoint"));
        }
    }
}
