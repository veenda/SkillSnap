using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly SkillSnapContext _context;
    private readonly IMemoryCache _cache;

    public ProjectsController(SkillSnapContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        if (!_cache.TryGetValue("project_list", out List<Project> projects))
        {
            projects = await _context.Projects
                .Include(p => p.PortfolioUser)
                .AsNoTracking()
                .Select(p => new Project
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    PortfolioUserId = p.PortfolioUserId,
                    PortfolioUser = p.PortfolioUser != null
                        ? new PortfolioUser
                        {
                            Id = p.PortfolioUser.Id,
                            Name = p.PortfolioUser.Name
                        }
                        : null
                })
                .ToListAsync();

            _cache.Set("project_list", projects, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5)));
        }

        return Ok(projects);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _context.Projects
            .Include(p => p.PortfolioUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Project>> AddProject([FromBody] Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        _cache.Remove("project_list");

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<Project>> UpdateProject(int id, [FromBody] Project project)
    {
        if (id != project.Id)
        {
            return BadRequest("Project ID mismatch");
        }

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        _cache.Remove("project_list");

        return Ok(project);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProject(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        _cache.Remove("project_list");

        return NoContent();
    }
}
