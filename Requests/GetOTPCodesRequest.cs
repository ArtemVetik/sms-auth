using Newtonsoft.Json;
using System.Text;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;

namespace Agava.SmsAuthServer
{
    internal class GetOTPCodesRequest : BaseRequest
    {
        public GetOTPCodesRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    SELECT *
                    FROM `otp_codes`;
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>()
                );
            });

            if (response.Status.IsSuccess == false)
                return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);

            var resultRows = ((ExecuteDataQueryResponse)response).Result.ResultSets[0].Rows;

            var otpCodes = new List<OtpCode>();
            foreach (var row in resultRows)
            {
                otpCodes.Add(new OtpCode() 
                {
                    phone = Encoding.UTF8.GetString(row["phone"].GetString()),
                    otp_code = row["otp_code"].GetUint32(),
                    expire_time = row["expire_time"].GetTimestamp()
                });
            }

            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), JsonConvert.SerializeObject(otpCodes), false);
        }

        class OtpCode
        {
            public string phone { get; set; }
            public uint otp_code { get; set; }
            public DateTime expire_time { get; set; }
        }
    }
}
