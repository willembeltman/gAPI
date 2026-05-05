using gAPI.Storage.Server.Config;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace gAPI.Storage.Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class AuthController(IOptions<LocalStorageServerConfig> config) : ControllerBase
{

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        //Console.WriteLine($"gAPI.Storage.Server.Controllers.AuthController / Login UserName={config.Value.Credentials.UserName} token={config.Value.SuperSecretKeyArray}");

        var cred = config.Value.Credentials.UserName == request.Username && config.Value.Credentials.Password == request.Password;

        if (!cred) return Unauthorized();
        var key = new SymmetricSecurityKey(config.Value.SuperSecretKeyArray);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new { token = jwt });
    }
}
