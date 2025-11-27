using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Models.Entities;

namespace YPrompt.Api.Services;

public interface IVersionService
{
    Task<CreateVersionData> CreateVersionAsync(int promptId, int userId, CreateVersionRequest data);
    Task<VersionListData> GetVersionHistoryAsync(int promptId, int userId, int page, int limit, string? versionTag);
    Task<VersionInfo> GetVersionDetailAsync(int promptId, int userId, int versionId);
    Task<VersionCompareData> CompareVersionsAsync(int promptId, int userId, int fromVersionId, int toVersionId);
    Task<RollbackData> RollbackToVersionAsync(int promptId, int userId, int versionId, string? changeSummary);
    Task UpdateVersionTagAsync(int promptId, int userId, int versionId, string versionTag);
    Task DeleteVersionAsync(int promptId, int userId, int versionId);
    string GenerateNextVersion(string currentVersion, string changeType);
}

public class VersionService : IVersionService
{
    private readonly YPromptDbContext _context;
    private readonly ILogger<VersionService> _logger;

    public VersionService(YPromptDbContext context, ILogger<VersionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string GenerateNextVersion(string currentVersion, string changeType)
    {
        try
        {
            var parts = currentVersion.Split('.');
            if (parts.Length != 3)
                return "1.0.1";

            var major = int.Parse(parts[0]);
            var minor = int.Parse(parts[1]);
            var patch = int.Parse(parts[2]);

            return changeType switch
            {
                "major" => $"{major + 1}.0.0",
                "minor" => $"{major}.{minor + 1}.0",
                _ => $"{major}.{minor}.{patch + 1}"
            };
        }
        catch
        {
            _logger.LogError("❌ 版本号生成失败");
            return "1.0.1";
        }
    }

    public async Task<CreateVersionData> CreateVersionAsync(int promptId, int userId, CreateVersionRequest data)
    {
        // Get current prompt
        var currentPrompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (currentPrompt == null)
        {
            throw new InvalidOperationException("提示词不存在或无权限");
        }

        // Generate new version number
        var newVersion = GenerateNextVersion(currentPrompt.CurrentVersion, data.ChangeType);

        // Create version snapshot
        var version = new PromptVersion
        {
            PromptId = promptId,
            VersionNumber = newVersion,
            VersionType = "manual",
            VersionTag = data.VersionTag,
            Title = currentPrompt.Title,
            Description = currentPrompt.Description,
            RequirementReport = currentPrompt.RequirementReport,
            ThinkingPoints = currentPrompt.ThinkingPoints,
            InitialPrompt = currentPrompt.InitialPrompt,
            Advice = currentPrompt.Advice,
            FinalPrompt = currentPrompt.FinalPrompt ?? string.Empty,
            Language = currentPrompt.Language,
            Format = currentPrompt.Format,
            Tags = currentPrompt.Tags,
            SystemPrompt = currentPrompt.SystemPrompt,
            ConversationHistory = currentPrompt.ConversationHistory,
            ChangeLog = data.ChangeLog,
            ChangeSummary = data.ChangeSummary ?? "版本更新",
            ChangeType = data.ChangeType,
            CreatedBy = userId,
            ContentSize = currentPrompt.FinalPrompt?.Length ?? 0
        };

        _context.PromptVersions.Add(version);

        // Update main table version info
        currentPrompt.CurrentVersion = newVersion;
        currentPrompt.TotalVersions++;
        currentPrompt.LastVersionTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 版本创建成功: prompt_id={PromptId}, version={Version}", promptId, newVersion);

        return new CreateVersionData
        {
            VersionId = version.Id,
            VersionNumber = newVersion,
            CreateTime = version.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<VersionListData> GetVersionHistoryAsync(int promptId, int userId, int page, int limit, string? versionTag)
    {
        // Verify permission
        var promptExists = await _context.Prompts
            .AnyAsync(p => p.Id == promptId && p.UserId == userId);

        if (!promptExists)
        {
            throw new InvalidOperationException("提示词不存在或无权限");
        }

        var query = _context.PromptVersions
            .Where(v => v.PromptId == promptId && v.IsDeleted == 0);

        if (!string.IsNullOrWhiteSpace(versionTag))
        {
            query = query.Where(v => v.VersionTag == versionTag);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.CreateTime)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(v => new VersionListItem
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                VersionTag = v.VersionTag,
                VersionType = v.VersionType,
                ChangeSummary = v.ChangeSummary,
                ContentSize = v.ContentSize,
                UseCount = v.UseCount,
                CreatedBy = v.CreatedBy,
                CreateTime = v.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                AuthorName = v.Creator != null ? v.Creator.Name : null,
                AuthorAvatar = v.Creator != null ? v.Creator.Avatar : null
            })
            .ToListAsync();

        _logger.LogDebug("✅ 查询版本列表成功: prompt_id={PromptId}, total={Total}", promptId, total);

        return new VersionListData
        {
            Total = total,
            Page = page,
            Limit = limit,
            Items = items
        };
    }

    public async Task<VersionInfo> GetVersionDetailAsync(int promptId, int userId, int versionId)
    {
        var version = await _context.PromptVersions
            .Include(v => v.Prompt)
            .Include(v => v.Creator)
            .FirstOrDefaultAsync(v =>
                v.Id == versionId &&
                v.PromptId == promptId &&
                v.Prompt!.UserId == userId &&
                v.IsDeleted == 0);

        if (version == null)
        {
            throw new InvalidOperationException("版本不存在或无权限");
        }

        _logger.LogDebug("✅ 获取版本详情成功: version_id={VersionId}", versionId);

        return MapToVersionInfo(version);
    }

    public async Task<VersionCompareData> CompareVersionsAsync(int promptId, int userId, int fromVersionId, int toVersionId)
    {
        var fromVersion = await GetVersionDetailAsync(promptId, userId, fromVersionId);
        var toVersion = await GetVersionDetailAsync(promptId, userId, toVersionId);

        var changes = new VersionCompareChanges
        {
            TitleChanged = fromVersion.Title != toVersion.Title,
            DescriptionChanged = fromVersion.Description != toVersion.Description,
            FinalPromptChanged = fromVersion.FinalPrompt != toVersion.FinalPrompt,
            TagsChanged = string.Join(",", fromVersion.Tags) != string.Join(",", toVersion.Tags)
        };

        _logger.LogDebug("✅ 版本对比成功: from={FromId}, to={ToId}", fromVersionId, toVersionId);

        return new VersionCompareData
        {
            FromVersion = new VersionCompareVersion
            {
                Id = fromVersion.Id,
                VersionNumber = fromVersion.VersionNumber,
                Title = fromVersion.Title,
                Description = fromVersion.Description,
                FinalPrompt = fromVersion.FinalPrompt,
                Tags = fromVersion.Tags,
                CreateTime = fromVersion.CreateTime,
                AuthorName = fromVersion.AuthorName
            },
            ToVersion = new VersionCompareVersion
            {
                Id = toVersion.Id,
                VersionNumber = toVersion.VersionNumber,
                Title = toVersion.Title,
                Description = toVersion.Description,
                FinalPrompt = toVersion.FinalPrompt,
                Tags = toVersion.Tags,
                CreateTime = toVersion.CreateTime,
                AuthorName = toVersion.AuthorName
            },
            Changes = changes,
            Diff = new VersionCompareDiff
            {
                FinalPrompt = new VersionDiffItem
                {
                    From = fromVersion.FinalPrompt ?? "",
                    To = toVersion.FinalPrompt ?? ""
                }
            }
        };
    }

    public async Task<RollbackData> RollbackToVersionAsync(int promptId, int userId, int versionId, string? changeSummary)
    {
        var targetVersion = await _context.PromptVersions
            .Include(v => v.Prompt)
            .FirstOrDefaultAsync(v =>
                v.Id == versionId &&
                v.PromptId == promptId &&
                v.Prompt!.UserId == userId &&
                v.IsDeleted == 0);

        if (targetVersion == null || targetVersion.Prompt == null)
        {
            throw new InvalidOperationException("版本不存在或无权限");
        }

        var prompt = targetVersion.Prompt;

        // Update prompt with version content
        prompt.Title = targetVersion.Title ?? prompt.Title;
        prompt.Description = targetVersion.Description;
        prompt.RequirementReport = targetVersion.RequirementReport;
        prompt.ThinkingPoints = targetVersion.ThinkingPoints;
        prompt.InitialPrompt = targetVersion.InitialPrompt;
        prompt.Advice = targetVersion.Advice;
        prompt.FinalPrompt = targetVersion.FinalPrompt;
        prompt.Language = targetVersion.Language;
        prompt.Format = targetVersion.Format;
        prompt.Tags = targetVersion.Tags;
        prompt.SystemPrompt = targetVersion.SystemPrompt;
        prompt.ConversationHistory = targetVersion.ConversationHistory;
        prompt.CurrentVersion = targetVersion.VersionNumber;
        prompt.LastVersionTime = DateTime.UtcNow;

        // Update rollback statistics
        targetVersion.RollbackCount++;
        targetVersion.UseCount++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 回滚成功: prompt_id={PromptId}, to_version={Version}",
            promptId, targetVersion.VersionNumber);

        return new RollbackData
        {
            NewVersion = targetVersion.VersionNumber,
            RollbackToVersion = targetVersion.VersionNumber
        };
    }

    public async Task UpdateVersionTagAsync(int promptId, int userId, int versionId, string versionTag)
    {
        var version = await _context.PromptVersions
            .Include(v => v.Prompt)
            .FirstOrDefaultAsync(v =>
                v.Id == versionId &&
                v.PromptId == promptId &&
                v.Prompt!.UserId == userId);

        if (version == null)
        {
            throw new InvalidOperationException("版本不存在或无权限");
        }

        version.VersionTag = versionTag;
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 更新版本标签成功: version_id={VersionId}, tag={Tag}", versionId, versionTag);
    }

    public async Task DeleteVersionAsync(int promptId, int userId, int versionId)
    {
        var version = await _context.PromptVersions
            .Include(v => v.Prompt)
            .FirstOrDefaultAsync(v =>
                v.Id == versionId &&
                v.PromptId == promptId &&
                v.Prompt!.UserId == userId &&
                v.IsDeleted == 0);

        if (version == null || version.Prompt == null)
        {
            throw new InvalidOperationException("版本不存在或无权限");
        }

        if (version.VersionTag == "initial")
        {
            throw new InvalidOperationException("不能删除初始版本");
        }

        if (version.VersionNumber == version.Prompt.CurrentVersion)
        {
            throw new InvalidOperationException("不能删除当前激活的版本");
        }

        // Soft delete
        version.IsDeleted = 1;
        version.Prompt.TotalVersions--;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 删除版本成功: version_id={VersionId}", versionId);
    }

    private static VersionInfo MapToVersionInfo(PromptVersion version)
    {
        return new VersionInfo
        {
            Id = version.Id,
            PromptId = version.PromptId,
            VersionNumber = version.VersionNumber,
            VersionTag = version.VersionTag,
            VersionType = version.VersionType,
            Title = version.Title,
            Description = version.Description,
            RequirementReport = version.RequirementReport,
            ThinkingPoints = ParseJsonArray(version.ThinkingPoints),
            InitialPrompt = version.InitialPrompt,
            Advice = ParseJsonArray(version.Advice),
            FinalPrompt = version.FinalPrompt,
            Language = version.Language,
            Format = version.Format,
            Tags = ParseTags(version.Tags),
            SystemPrompt = version.SystemPrompt,
            ConversationHistory = version.ConversationHistory,
            ChangeLog = version.ChangeLog,
            ChangeSummary = version.ChangeSummary,
            ChangeType = version.ChangeType,
            CreatedBy = version.CreatedBy,
            UseCount = version.UseCount,
            RollbackCount = version.RollbackCount,
            ContentSize = version.ContentSize,
            CreateTime = version.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            AuthorName = version.Creator?.Name,
            AuthorAvatar = version.Creator?.Avatar
        };
    }

    private static List<string> ParseTags(string? tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr))
            return new List<string>();

        return tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
