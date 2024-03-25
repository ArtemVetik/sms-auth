using Ydb.Sdk.Services.Table;

namespace Agava.SmsAuthServer
{
    internal class ErrorRequest : BaseRequest
    {
        private readonly Response _response;

        public ErrorRequest(TableClient tableClient, Request request, Response response) : base(tableClient, request)
        {
            _response = response;
        }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            await Task.Yield();

            return _response;
        }
    }
}
