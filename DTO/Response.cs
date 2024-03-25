namespace Agava.SmsAuthServer
{
    public class Response
    {
        public Response(uint statusCode, string reasonPhrase, string body, bool isBase64Encoded)
        {
            this.statusCode = statusCode;
            this.body = body;
            this.reasonPhrase = reasonPhrase;
            this.isBase64Encoded = isBase64Encoded;
        }

        public uint statusCode { get; private set; }
        public string reasonPhrase { get; private set; }
        public string body { get; private set; }
        public bool isBase64Encoded { get; private set; }
    }
}
