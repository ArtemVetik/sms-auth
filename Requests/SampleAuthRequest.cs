using Ydb.Sdk.Services.Table;

namespace Agava.SmsAuthServer
{
    internal class SampleAuthRequest : BaseRequest
    {
        public SampleAuthRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            await Task.Yield();

            try
            {
                var phone = JwtTokenService.Validate(request.access_token, JwtTokenService.TokenType.Access);
                return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), phone, false);
            }
            catch (Exception exception)
            {
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), exception.Message, false);
            }

        }
    }
}
