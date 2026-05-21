using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

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
                Description= p.Description,
                PortfolioUserId = p.PortfolioUserId,
                PortfolioUser = new PortfolioUser
                {
                    Id = p.PortfolioUser.Id,
                    Name = p.PortfolioUser.Name
                }
            }).ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set("project_list", projects, cacheOptions);
        }
        return Ok(projects);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Project>> AddProject([FromBody] Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        _cache.Remove("project_list"); // make sure user getting the latest data
        return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
    }
}