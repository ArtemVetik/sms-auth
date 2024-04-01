using System.Net;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Ydb.Sdk.Services.Table;

namespace Agava.SmsAuthServer
{
    internal class SetCloudSaveRequest : BaseRequest
    {
        private readonly AmazonDynamoDBClient _awsClient;

        public SetCloudSaveRequest(AmazonDynamoDBClient awsClient, TableClient tableClient, Request request) : base(tableClient, request)
        {
            _awsClient = awsClient;
        }

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

            var awsRequest = new BatchWriteItemRequest(new Dictionary<string, List<WriteRequest>>()
            {
                {
                    "cloud_saves", new List<WriteRequest>()
                    {
                        new WriteRequest(new PutRequest()
                        {
                            Item = new Dictionary<string, AttributeValue>
                            {
                                { "phone", new AttributeValue { S = phone } },
                                { "data", new AttributeValue { B = new MemoryStream(Encoding.UTF8.GetBytes(request.body)) } },
                            }
                        })
                    }
                }
            });

            var awsResponse = await _awsClient.BatchWriteItemAsync(awsRequest);

            if (awsResponse.HttpStatusCode != HttpStatusCode.OK)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Failed to write item!", false);

            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), string.Empty, false);
        }
    }
}
