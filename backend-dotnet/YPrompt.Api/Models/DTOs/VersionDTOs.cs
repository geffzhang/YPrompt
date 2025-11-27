namespace YPrompt.Api.Models.DTOs;

// Version DTOs

public class CreateVersionRequest
{
    public string ChangeType { get; set; } = "patch"; // major, minor, patch
    public string ChangeSummary { get; set; } = string.Empty;
    public string? ChangeLog { get; set; }
    public string? VersionTag { get; set; }
}

public class CreateVersionData
{
    public int VersionId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string CreateTime { get; set; } = string.Empty;
}

public class VersionListItem
{
    public int Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? VersionTag { get; set; }
    public string VersionType { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public int ContentSize { get; set; }
    public int UseCount { get; set; }
    public int? CreatedBy { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
}

public class VersionListData
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public List<VersionListItem> Items { get; set; } = new List<VersionListItem>();
}

public class VersionInfo
{
    public int Id { get; set; }
    public int PromptId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? VersionTag { get; set; }
    public string VersionType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? RequirementReport { get; set; }
    public List<string> ThinkingPoints { get; set; } = new List<string>();
    public string? InitialPrompt { get; set; }
    public List<string> Advice { get; set; } = new List<string>();
    public string FinalPrompt { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
    public string? SystemPrompt { get; set; }
    public string? ConversationHistory { get; set; }
    public string? ChangeLog { get; set; }
    public string? ChangeSummary { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public int UseCount { get; set; }
    public int RollbackCount { get; set; }
    public int ContentSize { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
}

public class VersionCompareData
{
    public VersionCompareVersion FromVersion { get; set; } = new VersionCompareVersion();
    public VersionCompareVersion ToVersion { get; set; } = new VersionCompareVersion();
    public VersionCompareChanges Changes { get; set; } = new VersionCompareChanges();
    public VersionCompareDiff Diff { get; set; } = new VersionCompareDiff();
}

public class VersionCompareVersion
{
    public int Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? FinalPrompt { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string CreateTime { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
}

public class VersionCompareChanges
{
    public bool TitleChanged { get; set; }
    public bool DescriptionChanged { get; set; }
    public bool FinalPromptChanged { get; set; }
    public bool TagsChanged { get; set; }
}

public class VersionCompareDiff
{
    public VersionDiffItem FinalPrompt { get; set; } = new VersionDiffItem();
}

public class VersionDiffItem
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public class RollbackRequest
{
    public string? ChangeSummary { get; set; }
}

public class RollbackData
{
    public string NewVersion { get; set; } = string.Empty;
    public string RollbackToVersion { get; set; } = string.Empty;
}

public class UpdateTagRequest
{
    public string VersionTag { get; set; } = string.Empty;
}
