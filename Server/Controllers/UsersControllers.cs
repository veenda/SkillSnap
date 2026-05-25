using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;
using System.Security.Claims;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public UsersController(
        SkillSnapContext context, 
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache) 
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PortfolioUser>> GetProfile()
    {
        var user = await _context.PortfolioUsers.FirstOrDefaultAsync();
        return user != null ? Ok(user) : NotFound();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var cacheKey = $"user_info_{userId}";

        if (!_cache.TryGetValue(cacheKey, out UserInfoDto? userInfo))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            userInfo = new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToList()
            };

            _cache.Set(cacheKey, userInfo, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30)));
        }

        return Ok(userInfo);
    }
}