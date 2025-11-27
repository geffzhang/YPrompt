using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("prompts")]
public class Prompt
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    // GPrompt four-step generation content
    [Column("requirement_report")]
    public string? RequirementReport { get; set; }

    [Column("thinking_points")]
    public string? ThinkingPoints { get; set; }

    [Column("initial_prompt")]
    public string? InitialPrompt { get; set; }

    [Column("advice")]
    public string? Advice { get; set; }

    [Column("final_prompt")]
    public string? FinalPrompt { get; set; }

    // Prompt configuration
    [Column("language")]
    [MaxLength(10)]
    public string Language { get; set; } = "zh";

    [Column("format")]
    [MaxLength(10)]
    public string Format { get; set; } = "markdown";

    [Column("prompt_type")]
    [MaxLength(10)]
    public string PromptType { get; set; } = "system";

    // User prompt specific fields
    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("conversation_history")]
    public string? ConversationHistory { get; set; }

    // Status flags
    [Column("is_favorite")]
    public int IsFavorite { get; set; } = 0;

    [Column("is_public")]
    public int IsPublic { get; set; } = 0;

    // Statistics
    [Column("view_count")]
    public int ViewCount { get; set; } = 0;

    [Column("use_count")]
    public int UseCount { get; set; } = 0;

    // Tags (comma-separated)
    [Column("tags")]
    [MaxLength(500)]
    public string? Tags { get; set; }

    // Version information
    [Column("current_version")]
    [MaxLength(20)]
    public string CurrentVersion { get; set; } = "1.0.0";

    [Column("total_versions")]
    public int TotalVersions { get; set; } = 1;

    [Column("last_version_time")]
    public DateTime? LastVersionTime { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("update_time")]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    public virtual ICollection<PromptVersion> Versions { get; set; } = new List<PromptVersion>();
    public virtual ICollection<PromptShare> Shares { get; set; } = new List<PromptShare>();
}
