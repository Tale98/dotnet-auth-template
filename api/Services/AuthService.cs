using Api.Data.Db;
using Api.Models.User;
using Api.Utils.JWT;
using Microsoft.EntityFrameworkCore;
using Api.Models.Exceptions;
namespace Api.Service.AuthService;
public interface IAuthService
{
    Task<User?> GetCurrentUserAsync();
}

public class AuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtUtils _jwtUtils;
    private readonly AppDbContext _dbContext;

    public AuthService(IHttpContextAccessor httpContextAccessor, JwtUtils jwtUtils, AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtUtils = jwtUtils;
        _dbContext = dbContext;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        string? token = context?.Request.Cookies["X-Access-Token"];

        if (string.IsNullOrEmpty(token)) throw new UnauthorizedException();

        int? userId = _jwtUtils.DecodeUserToken(token);
        if (userId == null) throw new UnauthorizedException();
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive) 
        {
            throw new UnauthorizedException();
        }

        return user;
    }
}