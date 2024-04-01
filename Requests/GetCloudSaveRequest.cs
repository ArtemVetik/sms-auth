using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Ydb.Sdk.Services.Table;

namespace Agava.SmsAuthServer
{
    internal class GetCloudSaveRequest : BaseRequest
    {
        private readonly AmazonDynamoDBClient _awsClient;

        public GetCloudSaveRequest(AmazonDynamoDBClient awsClient, TableClient tableClient, Request request) : base(tableClient, request)
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

            var awsRequest = new GetItemRequest("cloud_saves", new Dictionary<string, AttributeValue>()
            {
                { "phone", new AttributeValue {S = phone } }
            });

            var awsResponse = await _awsClient.GetItemAsync(awsRequest);

            if (awsResponse.HttpStatusCode != HttpStatusCode.OK)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Failed to get item!", false);

            if (awsResponse.Item.Count == 0)
                return new Response((uint)StatusCode.ValidationError, StatusCode.ValidationError.ToString(), "Record not found!", false);

            var data = awsResponse.Item["data"].B.ToArray();

            return new Response((uint)Ydb.Sdk.StatusCode.Success, Ydb.Sdk.StatusCode.Success.ToString(), Convert.ToBase64String(data), true);
        }
    }
}
