using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LE.ApiGateway.JwtHelpers
{
    public sealed class JwtTokenBuilder
    {
        private SecurityKey SecurityKey = null;
        private string Issuer = "";
        private string Audience = "";
        private List<Claim> Claims = new List<Claim>();
        private int ExpiryInDays = 1;

        public JwtTokenBuilder AddSecurityKey(SecurityKey securityKey)
        {
            SecurityKey = securityKey;
            return this;
        }

        public JwtTokenBuilder AddIssuer(string issuer)
        {
            Issuer = issuer;
            return this;
        }

        public JwtTokenBuilder AddAudience(string audience)
        {
            Audience = audience;
            return this;
        }

        public JwtTokenBuilder AddClaim(string type, string value)
        {
            Claims.Add(new Claim(type, value));
            return this;
        }

        public JwtTokenBuilder AddRole(string value)
        {
            Claims.Add(new Claim(ClaimTypes.Role, value));
            return this;
        }

        public JwtTokenBuilder AddClaims(List<Claim> claims)
        {
            Claims.AddRange(claims);
            return this;
        }

        public JwtTokenBuilder AddExpiry(int expiryInDays)
        {
            ExpiryInDays = expiryInDays;
            return this;
        }

        public JwtTokenBuilder AddExpiryInDays(int expiryInDays)
        {
            ExpiryInDays = expiryInDays;
            return this;
        }

        public JwtToken Build()
        {
            var token = new JwtSecurityToken(
                              issuer: Issuer,
                              audience: Audience,
                              claims: Claims,
                              expires: DateTime.UtcNow.AddDays(ExpiryInDays),
                              signingCredentials: new SigningCredentials(
                                                        SecurityKey,
                                                        SecurityAlgorithms.HmacSha256));

            return new JwtToken(token);
        }
    }
}
