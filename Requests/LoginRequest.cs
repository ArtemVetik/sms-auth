using Ydb.Sdk.Services.Table;
using Newtonsoft.Json;
using System.Text;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class LoginRequest : BaseRequest
    {
        public LoginRequest(TableClient tableClient, Request request)
            : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            var loginData = JsonConvert.DeserializeObject<LoginData>(request.body);

            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;
                    DECLARE $otp_code AS Uint32;

                    SELECT expire_time
                    FROM `otp_codes`
                    WHERE phone = $phone AND otp_code = $otp_code;

                    DELETE FROM `otp_codes`
                    WHERE phone = $phone AND otp_code = $otp_code;
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(loginData.phone)) },
                        { "$otp_code", YdbValue.MakeUint32(loginData.otp_code) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var resultRows = ((ExecuteDataQueryResponse)response).Result.ResultSets[0].Rows;

            if (resultRows.Count == 0)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Invalid credentials", false);

            var expireTime = resultRows[0]["expire_time"].GetTimestamp();

            if (DateTime.UtcNow > expireTime)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Invalid credentials", false);

            var accessToken = JwtTokenService.Create(loginData.phone, JwtTokenService.TokenType.Access);
            var refreshToken = JwtTokenService.Create(loginData.phone, JwtTokenService.TokenType.Refresh);

            response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;
                    DECLARE $refresh_token AS string;

                    UPSERT INTO `user_credentials` (phone, refresh_token)
                    VALUES ($phone, $refresh_token);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(loginData.phone)) },
                        { "$refresh_token", YdbValue.MakeString(Encoding.UTF8.GetBytes(refreshToken)) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var responseBody = new
            {
                access = accessToken,
                refresh = refreshToken
            };

            var jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseBody));
            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), Convert.ToBase64String(jsonBytes), true);
        }
    }
}
