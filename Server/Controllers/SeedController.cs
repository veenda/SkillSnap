using Microsoft.AspNetCore.Mvc;
using SkillSnap.Server.Data;
using SkillSnap.Shared.Models;

namespace SkillSnap.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly SkillSnapContext _context;

    public SeedController(SkillSnapContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> SeedDatabase()
    {
        if (_context.Projects.Any())
        {
            return BadRequest("Database already contains data! No need to seed again.");
        }

        // profile
        var adminProfile = new PortfolioUser
        {
            Name = "Veenda Putri Divo",
            Bio = "Data Science Student & Software Developer", 
            // Using a free API to generate a placeholder avatar with your initials
            ProfileImageUrl = "https://ui-avatars.com/api/?name=Veenda+Putri&background=0D8ABC&color=fff&size=256"
        };

        _context.Add(adminProfile);
        await _context.SaveChangesAsync();

        // projects
        _context.Projects.Add(new Project
        {
            Title = "SkillSnap",
            Description = "A text-based plantation simulator built with C#.",
            PortfolioUserId = adminProfile.Id
        });

        // skills
        _context.Skills.AddRange(
            new Skill { Name = "Data Science", Level = "Advanced", PortfolioUserId = adminProfile.Id },
            new Skill { Name = "Blazor WebAssembly", Level = "Intermediate", PortfolioUserId = adminProfile.Id }
        );

        await _context.SaveChangesAsync();

        return Ok("Seed complete! You can now close this tab and refresh your Blazor app.");
    }
}