using System.Text;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class GetCloudSaveRequest : BaseRequest
    {
        public GetCloudSaveRequest(TableClient tableClient, Request request) : base(tableClient, request)
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

                    SELECT data
                    FROM `cloud_saves`
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

            if (resultRows.Count == 0)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var data = resultRows[0]["data"].GetJson();

            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), data, false);
        }
    }
}
