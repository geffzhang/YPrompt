namespace YPrompt.Api.Models.DTOs;

// Prompt Rules DTOs

public class PromptRulesData
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? SystemPromptRules { get; set; }
    public string? UserGuidedPromptRules { get; set; }
    public string? RequirementReportRules { get; set; }
    public string? ThinkingPointsExtractionPrompt { get; set; }
    public string? ThinkingPointsSystemMessage { get; set; }
    public string? SystemPromptGenerationPrompt { get; set; }
    public string? SystemPromptSystemMessage { get; set; }
    public string? OptimizationAdvicePrompt { get; set; }
    public string? OptimizationAdviceSystemMessage { get; set; }
    public string? OptimizationApplicationPrompt { get; set; }
    public string? OptimizationApplicationSystemMessage { get; set; }
    public string? QualityAnalysisSystemPrompt { get; set; }
    public string? UserPromptQualityAnalysis { get; set; }
    public string? UserPromptQuickOptimization { get; set; }
    public string? UserPromptRulesContent { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
}

public class SavePromptRulesRequest
{
    public string? SystemPromptRules { get; set; }
    public string? UserGuidedPromptRules { get; set; }
    public string? RequirementReportRules { get; set; }
    public string? ThinkingPointsExtractionPrompt { get; set; }
    public string? ThinkingPointsSystemMessage { get; set; }
    public string? SystemPromptGenerationPrompt { get; set; }
    public string? SystemPromptSystemMessage { get; set; }
    public string? OptimizationAdvicePrompt { get; set; }
    public string? OptimizationAdviceSystemMessage { get; set; }
    public string? OptimizationApplicationPrompt { get; set; }
    public string? OptimizationApplicationSystemMessage { get; set; }
    public string? QualityAnalysisSystemPrompt { get; set; }
    public string? UserPromptQualityAnalysis { get; set; }
    public string? UserPromptQuickOptimization { get; set; }
    public string? UserPromptRulesContent { get; set; }
}
