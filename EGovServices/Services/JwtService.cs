using EGovServices.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EGovServices.Application.Common.Interfaces;

namespace EGovServices.API.Services;



public sealed class JwtService(IConfiguration config) : IJwtService
{
public string GenerateToken(User user)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.PhoneNumber),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("NationalNumber", user.NationalNumber),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(
            int.Parse(config["Jwt:ExpireMinutes"] ?? "60")),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
}