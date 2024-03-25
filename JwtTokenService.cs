using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Agava.SmsAuthServer
{
    internal static class JwtTokenService
    {
        private static readonly string Secret = Environment.GetEnvironmentVariable("JwtSalt");
        private static readonly string SecurityAlgorithm = SecurityAlgorithms.HmacSha256Signature;
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromMinutes(15);

        public static string Create(string id, TokenType tokenType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", id), new Claim("type", tokenType.ToString()) }),
                Expires = DateTime.UtcNow.Add(tokenType == TokenType.Access ? AccessTokenLifetime : RefreshTokenLifetime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithm)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string Validate(string token, TokenType tokenType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var type = jwtToken.Claims.First(x => x.Type == "type").Value;

            if (tokenType.ToString() != type)
                throw new Exception("Validation error");

            return jwtToken.Claims.First(x => x.Type == "id").Value;
        }

        public enum TokenType
        {
            Access,
            Refresh,
        }
    }
}
