using Yandex.Cloud.Credentials;
using Yandex.Cloud.Functions;
using Ydb.Sdk.Auth;
using Ydb.Sdk;
using Ydb.Sdk.Services.Table;
using SmsAuthServer;
using Amazon.DynamoDBv2;
using Amazon;

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

            var awsConfig = new AmazonDynamoDBConfig()
            {
                RegionEndpoint = RegionEndpoint.EUCentral1,
                EndpointProvider = new EndpointProvider(),
            };

            var awsAccessKeyId = Environment.GetEnvironmentVariable("AwsAccessKeyId");
            var awsSecretAccessKey = Environment.GetEnvironmentVariable("AwsSecretAccessKey");

            var awsClient = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsConfig);

            try
            {
                BaseRequest requestHandler = request.method switch
                {
                    "LOGIN" => new LoginRequest(tableClient, request),
                    "REGISTRATION" => new RegistrationRequest(tableClient, request),
                    "REFRESH" => new RefreshRequest(tableClient, request),
                    "UNLINK" => new UnlinkRequest(tableClient, request),
                    "GET_CLOUD_SAVES" => new GetCloudSaveRequest(awsClient, tableClient, request),
                    "SET_CLOUD_SAVES" => new SetCloudSaveRequest(awsClient, tableClient, request),
                    "SAMPLE_AUTH" => new SampleAuthRequest(tableClient, request),
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
