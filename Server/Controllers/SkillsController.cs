using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;

    public SkillsController(SkillSnapContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        if (!_cache.TryGetValue("skill_list", out List<Skill> skills))
        {
            skills = await _context.Skills
                .Include(s => s.PortfolioUser)
                .AsNoTracking()
                .Select(s => new Skill
                {
                    Id = s.Id,
                    Name = s.Name,
                    Level = s.Level,
                    PortfolioUserId = s.PortfolioUserId,
                    PortfolioUser = s.PortfolioUser != null
                        ? new PortfolioUser
                        {
                            Id = s.PortfolioUser.Id,
                            Name = s.PortfolioUser.Name
                        }
                        : null
                })
                .ToListAsync();

            _cache.Set("skill_list", skills, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
        }

        return Ok(skills);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var skill = await _context.Skills
            .Include(s => s.PortfolioUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null)
        {
            return NotFound();
        }

        return Ok(skill);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Skill>> AddSkill([FromBody] Skill skill)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        _cache.Remove("skill_list");

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<Skill>> UpdateSkill(int id, [FromBody] Skill skill)
    {
        if (id != skill.Id)
        {
            return BadRequest("Skill ID mismatch");
        }

        _context.Skills.Update(skill);
        await _context.SaveChangesAsync();
        _cache.Remove("skill_list");

        return Ok(skill);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound();
        }

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
        _cache.Remove("skill_list");

        return NoContent();
    }
}
