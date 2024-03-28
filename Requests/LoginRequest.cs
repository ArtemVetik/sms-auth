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

            var otpResponse = await ValidateAndClearOTPCode(client, loginData);

            if (otpResponse.statusCode != (uint)Ydb.Sdk.StatusCode.Success)
                return otpResponse;

            var accessToken = JwtTokenService.Create(loginData.phone, JwtTokenService.TokenType.Access);
            var refreshToken = JwtTokenService.Create(loginData.phone, JwtTokenService.TokenType.Refresh);

            if (await CanAuthorize(client, loginData))
            {
                var upsertResponse = await UpsertUser(client, loginData, refreshToken);

                if (upsertResponse.statusCode != (uint)Ydb.Sdk.StatusCode.Success)
                    return upsertResponse;
            }
            else
            {
                refreshToken = string.Empty;
            }

            var responseBody = new
            {
                access = accessToken,
                refresh = refreshToken
            };

            var jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseBody));
            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), Convert.ToBase64String(jsonBytes), true);
        }

        private async Task<Response> ValidateAndClearOTPCode(TableClient client, LoginData loginData)
        {
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
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Invalid credentials1", false);

            var expireTime = resultRows[0]["expire_time"].GetTimestamp();

            if (DateTime.UtcNow > expireTime)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Invalid credentials2", false);

            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), string.Empty, false);
        }

        private async Task<bool> CanAuthorize(TableClient client, LoginData loginData)
        {
            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;

                    SELECT device_id
                    FROM `user_credentials`
                    WHERE phone = $phone;
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(loginData.phone)) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return false;

            var resultRows = ((ExecuteDataQueryResponse)response).Result.ResultSets[0].Rows;

            foreach (var row in resultRows)
                if (row["device_id"].GetString() == Encoding.UTF8.GetBytes(loginData.device_id))
                    return true;

            return resultRows.Count < 5;
        }

        private async Task<Response> UpsertUser(TableClient client, LoginData loginData, string refreshToken)
        {
            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;
                    DECLARE $device_id AS string;
                    DECLARE $refresh_token AS string;

                    UPSERT INTO `user_credentials` (phone, device_id, refresh_token)
                    VALUES ($phone, $device_id, $refresh_token);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(loginData.phone)) },
                        { "$device_id", YdbValue.MakeString(Encoding.UTF8.GetBytes(loginData.device_id)) },
                        { "$refresh_token", YdbValue.MakeString(Encoding.UTF8.GetBytes(refreshToken)) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), string.Empty, false);
        }
    }
}
