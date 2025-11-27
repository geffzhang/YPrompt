using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("prompt_versions")]
public class PromptVersion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("prompt_id")]
    public int PromptId { get; set; }

    // Version identifier
    [Required]
    [Column("version_number")]
    [MaxLength(20)]
    public string VersionNumber { get; set; } = string.Empty;

    [Column("version_type")]
    [MaxLength(10)]
    public string VersionType { get; set; } = "manual";

    [Column("version_tag")]
    [MaxLength(50)]
    public string? VersionTag { get; set; }

    // Complete content snapshot
    [Column("title")]
    [MaxLength(200)]
    public string? Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("requirement_report")]
    public string? RequirementReport { get; set; }

    [Column("thinking_points")]
    public string? ThinkingPoints { get; set; }

    [Column("initial_prompt")]
    public string? InitialPrompt { get; set; }

    [Column("advice")]
    public string? Advice { get; set; }

    [Required]
    [Column("final_prompt")]
    public string FinalPrompt { get; set; } = string.Empty;

    [Column("language")]
    [MaxLength(10)]
    public string Language { get; set; } = "zh";

    [Column("format")]
    [MaxLength(10)]
    public string Format { get; set; } = "markdown";

    [Column("tags")]
    [MaxLength(500)]
    public string? Tags { get; set; }

    // User prompt context
    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("conversation_history")]
    public string? ConversationHistory { get; set; }

    // Version metadata
    [Column("change_log")]
    public string? ChangeLog { get; set; }

    [Column("change_summary")]
    [MaxLength(500)]
    public string? ChangeSummary { get; set; }

    [Column("change_type")]
    [MaxLength(10)]
    public string ChangeType { get; set; } = "patch";

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("parent_version_id")]
    public int? ParentVersionId { get; set; }

    // Statistics
    [Column("use_count")]
    public int UseCount { get; set; } = 0;

    [Column("rollback_count")]
    public int RollbackCount { get; set; } = 0;

    [Column("content_size")]
    public int ContentSize { get; set; } = 0;

    // Flags
    [Column("is_auto_save")]
    public int IsAutoSave { get; set; } = 0;

    [Column("is_deleted")]
    public int IsDeleted { get; set; } = 0;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PromptId")]
    public virtual Prompt? Prompt { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? Creator { get; set; }
}
