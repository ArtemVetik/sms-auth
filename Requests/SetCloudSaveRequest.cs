using System.Text;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class SetCloudSaveRequest : BaseRequest
    {
        public SetCloudSaveRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
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
                    DECLARE $phone AS String;
                    DECLARE $data AS Json;

                    UPSERT INTO `cloud_saves` (phone, data)
                    VALUES ($phone, $data);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(phone)) },
                        { "$data", YdbValue.MakeJson(request.body) },
                    }
                );
            });

            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);
        }
    }
}
