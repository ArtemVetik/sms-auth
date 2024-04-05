using System.Text;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class GetRemoteConfigRequest : BaseRequest
    {
        public GetRemoteConfigRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $key AS string;
                    
                    SELECT value
                    FROM `remote_config`
                    WHERE key = $key;
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>()
                    {
                        { "$key", YdbValue.MakeString(Encoding.UTF8.GetBytes(request.body)) },
                    }
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var resultRows = ((ExecuteDataQueryResponse)response).Result.ResultSets[0].Rows;

            if (resultRows.Count == 0)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var value = resultRows[0]["value"].GetString();
            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), Encoding.UTF8.GetString(value), false);
        }
    }
}
