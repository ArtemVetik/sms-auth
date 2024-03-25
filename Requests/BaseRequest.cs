using Ydb.Sdk.Services.Table;

namespace Agava.SmsAuthServer
{
    internal abstract class BaseRequest
    {
        private readonly TableClient _tableClient;
        private readonly Request _request;

        public BaseRequest(TableClient tableClient, Request request)
        {
            _tableClient = tableClient;
            _request = request;
        }

        public async Task<Response> Handle() => await Handle(_tableClient, _request);

        protected abstract Task<Response> Handle(TableClient client, Request request);
    }
}
