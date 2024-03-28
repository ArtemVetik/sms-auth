using Ydb.Sdk.Services.Table;
using System.Text;
using Ydb.Sdk.Value;
using SmsAuthServer;

namespace Agava.SmsAuthServer
{
    internal class RegistrationRequest : BaseRequest
    {
        public RegistrationRequest(TableClient tableClient, Request request) : base(tableClient, request)
        { }

        protected override async Task<Response> Handle(TableClient client, Request request)
        {
            uint otpCode = OtpCodeFactory.CreateNew();

            // sending an SMS with a code to the client
            // ...

            var response = await client.SessionExec(async session =>
            {
                var query = $@"
                    DECLARE $phone AS string;
                    DECLARE $otp_code AS Uint32;
                    DECLARE $expire_time AS Timestamp;

                    UPSERT INTO `otp_codes` (phone, otp_code, expire_time)
                    VALUES ($phone, $otp_code, $expire_time);
                ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                    {
                        { "$phone", YdbValue.MakeString(Encoding.UTF8.GetBytes(request.body)) },
                        { "$otp_code", YdbValue.MakeUint32(otpCode) },
                        { "$expire_time", YdbValue.MakeTimestamp(DateTime.Now.AddMinutes(15)) },
                    }
                );
            });

            return new Response((uint)response.Status.StatusCode, response.Status.StatusCode.ToString(), string.Empty, false);
        }
    }
}
