using Yandex.Cloud.Credentials;
using Yandex.Cloud.Functions;
using Ydb.Sdk.Auth;
using Ydb.Sdk;
using Ydb.Sdk.Services.Table;
using SmsAuthServer;

namespace Agava.SmsAuthServer
{
    internal class Handler : YcFunction<Request, Task<Response>>
    {
        public async Task<Response> FunctionHandler(Request request, Context context)
        {
            string ydbEndpoint = Environment.GetEnvironmentVariable("YdbEndpoint");
            string ydbDatabase = Environment.GetEnvironmentVariable("YdbDatabase");

            var token = new TokenProvider(new MetadataCredentialsProvider().GetToken());
            var config = new DriverConfig(ydbEndpoint, ydbDatabase, token);

            var driver = new Driver(config);
            await driver.Initialize();

            var tableClient = new TableClient(driver, new TableClientConfig());

            try
            {
                BaseRequest requestHandler = request.method switch
                {
                    "LOGIN" => new LoginRequest(tableClient, request),
                    "REGISTRATION" => new RegistrationRequest(tableClient, request),
                    "REFRESH" => new RefreshRequest(tableClient, request),
                    "UNLINK" => new UnlinkRequest(tableClient, request),
                    "GET_CLOUD_SAVES" => new GetCloudSaveRequest(tableClient, request),
                    "SET_CLOUD_SAVES" => new SetCloudSaveRequest(tableClient, request),
                    "GET_DEVICES" => new GetDevicesRequest(tableClient, request),
                    "GET_REMOTE_CONFIG" => new GetRemoteConfigRequest(tableClient, request),
                    "SAMPLE_AUTH" => new SampleAuthRequest(tableClient, request),
                    "GET_OTP_CODES" => new GetOTPCodesRequest(tableClient, request),
                    _ => new ErrorRequest(tableClient, request,
                        new Response((uint)StatusCode.NotFound, StatusCode.NotFound.ToString(), $"Method {request.method} not found", false)),
                };

                return await requestHandler.Handle();
            }
            finally
            {
                tableClient.Dispose();
                driver.Dispose();
            }
        }
    }
}
