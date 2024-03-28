using Agava.SmsAuthServer;
using System.Text;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace SmsAuthServer
{
    internal class UnlinkRequest : BaseRequest
    {
        public UnlinkRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected async override Task<Response> Handle(TableClient client, Request request)
        {
            string phone;
            try
            {
                phone = JwtTokenService.Validate(request.access_token, JwtTokenService.TokenType.Access);
            }
            catch (Exception exception)
            {
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), exception.Message, false);
            }

            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;
                    DECLARE $device_id AS string;

                    DELETE FROM `user_credentials`
                    WHERE phone = $phone AND device_id = $device_id
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(phone)) },
                        { "$device_id", YdbValue.MakeString(Encoding.UTF8.GetBytes(request.body)) }
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), string.Empty, false);
        }
    }
}
