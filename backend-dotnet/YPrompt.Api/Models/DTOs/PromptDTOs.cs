using System.Text.Json.Serialization;

namespace YPrompt.Api.Models.DTOs;

// Prompt DTOs

public class SavePromptRequest
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RequirementReport { get; set; }
    public List<string>? ThinkingPoints { get; set; }
    public string? InitialPrompt { get; set; }
    public List<string>? Advice { get; set; }
    public string? FinalPrompt { get; set; }
    public string Language { get; set; } = "zh";
    public string Format { get; set; } = "markdown";
    public string PromptType { get; set; } = "system";
    public List<string>? Tags { get; set; }
    public int? IsPublic { get; set; }
    public string? SystemPrompt { get; set; }
    public string? ConversationHistory { get; set; }
    public bool CreateVersion { get; set; } = true;
    public string? ChangeSummary { get; set; }
    public string? ChangeType { get; set; }
    public string? ChangeLog { get; set; }
    public string? VersionTag { get; set; }
}

public class PromptListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FinalPrompt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string PromptType { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string? ConversationHistory { get; set; }
    public int IsFavorite { get; set; }
    public int IsPublic { get; set; }
    public int ViewCount { get; set; }
    public int UseCount { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string CurrentVersion { get; set; } = string.Empty;
    public int TotalVersions { get; set; }
    public string LastVersionTime { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
}

public class PromptInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RequirementReport { get; set; }
    public List<string> ThinkingPoints { get; set; } = new List<string>();
    public string? InitialPrompt { get; set; }
    public List<string> Advice { get; set; } = new List<string>();
    public string? FinalPrompt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string PromptType { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string? ConversationHistory { get; set; }
    public int IsFavorite { get; set; }
    public int IsPublic { get; set; }
    public int ViewCount { get; set; }
    public int UseCount { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string CurrentVersion { get; set; } = string.Empty;
    public int TotalVersions { get; set; }
    public string LastVersionTime { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
    public string UpdateTime { get; set; } = string.Empty;
}

public class PromptListData
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public List<PromptListItem> Items { get; set; } = new List<PromptListItem>();
}

public class SavePromptData
{
    public int Id { get; set; }
    public bool IsNew { get; set; }
    public string? Version { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class FavoriteRequest
{
    public bool IsFavorite { get; set; }
}
