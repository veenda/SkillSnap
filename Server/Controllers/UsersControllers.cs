using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly SkillSnapContext _context;

    public UsersController(SkillSnapContext context) => _context = context;

    [HttpGet("profile")]
    public async Task<ActionResult<PortfolioUser>> GetProfile()
    {
        var user = await _context.PortfolioUsers.FirstOrDefaultAsync();
        return user != null ? Ok(user) : NotFound();
    }
}