using Ydb.Sdk.Services.Table;
using Newtonsoft.Json;
using System.Text;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class RefreshRequest : BaseRequest
    {
        public RefreshRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            string phone;
            try
            {
                phone = JwtTokenService.Validate(request.body, JwtTokenService.TokenType.Refresh);
            }
            catch (Exception exception)
            {
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), exception.Message, false);
            }

            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;

                    SELECT refresh_token
                    FROM `user_credentials`
                    WHERE phone = $phone;
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(phone)) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var resultRows = ((ExecuteDataQueryResponse)response).Result.ResultSets[0].Rows;

            bool hasRefreshToken = false;
            foreach (var row in resultRows)
            {
                var dbToken = row["refresh_token"].GetString();
                
                if (Encoding.UTF8.GetString(dbToken) == request.body)
                {
                    hasRefreshToken = true;
                    break;
                }
            }

            if (hasRefreshToken == false)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Invalid token", false);

            var responseBody = new
            {
                access = JwtTokenService.Create(phone, JwtTokenService.TokenType.Access),
                refresh = request.body
            };

            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), JsonConvert.SerializeObject(responseBody), false);
        }
    }
}
