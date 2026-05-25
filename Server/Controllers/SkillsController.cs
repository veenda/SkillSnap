using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;
using System.Diagnostics;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        var sw = Stopwatch.StartNew(); // Start timer

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

        sw.Stop(); // Stop timer
        Debug.WriteLine($"GetSkills took {sw.ElapsedMilliseconds} ms");
        return Ok(skills);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var cacheKey = $"skill_{id}";
        
        if (!_cache.TryGetValue(cacheKey, out Skill? skill))
        {
            skill = await _context.Skills
                .Include(s => s.PortfolioUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (skill == null)
            {
                return NotFound();
            }

            _cache.Set(cacheKey, skill, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
        }

        return Ok(skill);
    }

    // Admin only

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Skill>> AddSkill([FromBody] Skill? skill)
    {
        if (skill == null)
        {
            return BadRequest();
        }

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        _cache.Remove("skill_list");

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(int id, [FromBody] Skill? skill)
    {
        if (skill == null)
        {
            return BadRequest();
        }

        if (id != skill.Id)
        {
            return BadRequest();
        }

        _context.Entry(skill).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _cache.Remove("skill_list");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Skills.Any(s => s.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(int id)
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