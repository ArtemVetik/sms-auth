
namespace SmsAuthServer
{
    internal static class OtpCodeFactory
    {
        private static readonly byte Length = 6;
        private static readonly string Numbers = "0123456789";
        private static readonly Random Random = new Random();

        public static uint CreateNew()
        {
            string otp = string.Empty;

            for (byte i = 0; i < Length; i++)
                otp += Numbers[Random.Next(0, Numbers.Length)];

            return Convert.ToUInt32(otp);
        }
    }
}
