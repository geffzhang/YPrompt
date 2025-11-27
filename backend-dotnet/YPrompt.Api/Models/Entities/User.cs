using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // Linux.do OAuth fields
    [Column("linux_do_id")]
    [MaxLength(64)]
    public string? LinuxDoId { get; set; }

    [Column("linux_do_username")]
    [MaxLength(100)]
    public string? LinuxDoUsername { get; set; }

    // Local authentication fields
    [Column("username")]
    [MaxLength(50)]
    public string? Username { get; set; }

    [Column("password_hash")]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    // Common fields
    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("avatar")]
    [MaxLength(500)]
    public string? Avatar { get; set; }

    [Column("email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    [Column("auth_type")]
    [MaxLength(10)]
    public string AuthType { get; set; } = "linux_do";

    [Column("is_active")]
    public int IsActive { get; set; } = 1;

    [Column("is_admin")]
    public int IsAdmin { get; set; } = 0;

    [Column("last_login_time")]
    public DateTime? LastLoginTime { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("update_time")]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
    public virtual ICollection<PromptTag> PromptTags { get; set; } = new List<PromptTag>();
    public virtual UserPromptRules? UserPromptRules { get; set; }
}
