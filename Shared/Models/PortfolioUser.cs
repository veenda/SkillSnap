using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

// Id (int)
// Name (string)
// Bio (string)
// ProfileImageUrl (string)
// Navigation: List<Project>, List<Skill>

namespace SkillSnap.Shared.Models
{
    public class PortfolioUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Skill> Skills { get; set; } = new List<Skill>();
    }
} 