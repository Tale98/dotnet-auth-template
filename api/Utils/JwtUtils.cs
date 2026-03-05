using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Models.User;
using Microsoft.IdentityModel.Tokens;
namespace Api.Utils.JWT;

public class JwtUtils
{
    private readonly IConfiguration _config;

    public JwtUtils(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateUserToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public int? DecodeUserToken(string tokenString)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);
        string userIdString = jwtToken.Claims.First(c => c.Type == "sub").Value;
        int userIdInt;
        if (int.TryParse(userIdString, out userIdInt))
        {
            return userIdInt;
        }
        else
        {
            return null;
        }
    }
}