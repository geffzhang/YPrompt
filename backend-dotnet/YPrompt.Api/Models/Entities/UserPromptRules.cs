using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YPrompt.Api.Models.Entities;

[Table("user_prompt_rules")]
public class UserPromptRules
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    // Prompt rules content (JSON format)
    [Column("system_prompt_rules")]
    public string? SystemPromptRules { get; set; }

    [Column("user_guided_prompt_rules")]
    public string? UserGuidedPromptRules { get; set; }

    [Column("requirement_report_rules")]
    public string? RequirementReportRules { get; set; }

    [Column("thinking_points_extraction_prompt")]
    public string? ThinkingPointsExtractionPrompt { get; set; }

    [Column("thinking_points_system_message")]
    public string? ThinkingPointsSystemMessage { get; set; }

    [Column("system_prompt_generation_prompt")]
    public string? SystemPromptGenerationPrompt { get; set; }

    [Column("system_prompt_system_message")]
    public string? SystemPromptSystemMessage { get; set; }

    [Column("optimization_advice_prompt")]
    public string? OptimizationAdvicePrompt { get; set; }

    [Column("optimization_advice_system_message")]
    public string? OptimizationAdviceSystemMessage { get; set; }

    [Column("optimization_application_prompt")]
    public string? OptimizationApplicationPrompt { get; set; }

    [Column("optimization_application_system_message")]
    public string? OptimizationApplicationSystemMessage { get; set; }

    [Column("quality_analysis_system_prompt")]
    public string? QualityAnalysisSystemPrompt { get; set; }

    [Column("user_prompt_quality_analysis")]
    public string? UserPromptQualityAnalysis { get; set; }

    [Column("user_prompt_quick_optimization")]
    public string? UserPromptQuickOptimization { get; set; }

    [Column("user_prompt_rules")]
    public string? UserPromptRulesContent { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("update_time")]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
