using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Models.Entities;

namespace YPrompt.Api.Services;

public interface IPromptService
{
    Task<SavePromptData> SavePromptAsync(int userId, SavePromptRequest data);
    Task<PromptListData> GetPromptsListAsync(int userId, int page, int limit, string? keyword, string? tag, string? isFavorite, string sort);
    Task<PromptInfo?> GetPromptDetailAsync(int userId, int promptId);
    Task<bool> UpdatePromptAsync(int userId, int promptId, SavePromptRequest data);
    Task<bool> DeletePromptAsync(int userId, int promptId);
    Task<bool> ToggleFavoriteAsync(int userId, int promptId, bool isFavorite);
    Task IncreaseViewCountAsync(int promptId);
    Task<bool> IncreaseUseCountAsync(int userId, int promptId);
}

public class PromptService : IPromptService
{
    private readonly YPromptDbContext _context;
    private readonly IVersionService _versionService;
    private readonly ILogger<PromptService> _logger;

    public PromptService(
        YPromptDbContext context,
        IVersionService versionService,
        ILogger<PromptService> logger)
    {
        _context = context;
        _versionService = versionService;
        _logger = logger;
    }

    public async Task<SavePromptData> SavePromptAsync(int userId, SavePromptRequest data)
    {
        if (data.Id.HasValue && data.Id.Value > 0)
        {
            // Update existing prompt
            var existingPrompt = await _context.Prompts
                .FirstOrDefaultAsync(p => p.Id == data.Id.Value && p.UserId == userId);

            if (existingPrompt == null)
            {
                throw new UnauthorizedAccessException("提示词不存在或无权限修改");
            }

            _logger.LogInformation("🔄 更新提示词: prompt_id={PromptId}", data.Id.Value);
            await UpdatePromptAsync(userId, data.Id.Value, data);

            string? versionNumber = null;
            if (data.CreateVersion)
            {
                var versionData = new CreateVersionRequest
                {
                    ChangeType = data.ChangeType ?? "patch",
                    ChangeSummary = data.ChangeSummary ?? $"更新提示词({data.ChangeType ?? "patch"})",
                    ChangeLog = data.ChangeLog,
                    VersionTag = data.VersionTag ?? "stable"
                };

                var versionResult = await _versionService.CreateVersionAsync(data.Id.Value, userId, versionData);
                versionNumber = versionResult.VersionNumber;
                _logger.LogInformation("✅ 版本创建成功: version={Version}", versionNumber);
            }

            return new SavePromptData
            {
                Id = data.Id.Value,
                IsNew = false,
                Version = versionNumber,
                Message = "更新成功" + (versionNumber != null ? $",版本 {versionNumber}" : "")
            };
        }
        else
        {
            // Create new prompt
            _logger.LogInformation("📝 创建新提示词");
            var promptId = await CreatePromptAsync(userId, data);

            if (data.CreateVersion)
            {
                var versionData = new CreateVersionRequest
                {
                    ChangeType = "minor",
                    ChangeSummary = data.ChangeSummary ?? "初始版本",
                    ChangeLog = "创建提示词",
                    VersionTag = "initial"
                };

                await _versionService.CreateVersionAsync(promptId, userId, versionData);
                _logger.LogInformation("✅ 初始版本创建成功: version=1.0.0");
            }

            return new SavePromptData
            {
                Id = promptId,
                IsNew = true,
                Version = data.CreateVersion ? "1.0.0" : null,
                Message = "创建成功" + (data.CreateVersion ? ",版本 1.0.0" : "")
            };
        }
    }

    private async Task<int> CreatePromptAsync(int userId, SavePromptRequest data)
    {
        var thinkingPointsJson = data.ThinkingPoints != null
            ? JsonSerializer.Serialize(data.ThinkingPoints)
            : null;

        var adviceJson = data.Advice != null
            ? JsonSerializer.Serialize(data.Advice)
            : null;

        var tagsStr = data.Tags != null
            ? string.Join(",", data.Tags)
            : null;

        var prompt = new Prompt
        {
            UserId = userId,
            Title = data.Title ?? "未命名提示词",
            Description = data.Description,
            RequirementReport = data.RequirementReport,
            ThinkingPoints = thinkingPointsJson,
            InitialPrompt = data.InitialPrompt,
            Advice = adviceJson,
            FinalPrompt = data.FinalPrompt,
            Language = data.Language ?? "zh",
            Format = data.Format ?? "markdown",
            PromptType = data.PromptType ?? "system",
            IsFavorite = 0,
            IsPublic = data.IsPublic ?? 0,
            Tags = tagsStr,
            CurrentVersion = "1.0.0",
            TotalVersions = 1,
            LastVersionTime = DateTime.UtcNow,
            SystemPrompt = data.PromptType == "user" ? data.SystemPrompt : null,
            ConversationHistory = data.PromptType == "user" ? data.ConversationHistory : null
        };

        _context.Prompts.Add(prompt);
        await _context.SaveChangesAsync();

        // Update tags statistics
        if (data.Tags != null && data.Tags.Count > 0)
        {
            await UpdateTagsAsync(userId, data.Tags);
        }

        _logger.LogInformation("✅ 提示词创建成功: prompt_id={Id}, user_id={UserId}, title={Title}",
            prompt.Id, userId, prompt.Title);

        return prompt.Id;
    }

    public async Task<PromptListData> GetPromptsListAsync(int userId, int page, int limit, string? keyword, string? tag, string? isFavorite, string sort)
    {
        var query = _context.Prompts
            .Where(p => p.UserId == userId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                (p.Title != null && p.Title.Contains(keyword)) ||
                (p.Description != null && p.Description.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(p => p.Tags != null && p.Tags.Contains(tag));
        }

        if (!string.IsNullOrWhiteSpace(isFavorite) && int.TryParse(isFavorite, out int favValue))
        {
            query = query.Where(p => p.IsFavorite == favValue);
        }

        // Get total count
        var total = await query.CountAsync();

        // Apply sorting
        query = sort switch
        {
            "update_time" => query.OrderByDescending(p => p.UpdateTime),
            "view_count" => query.OrderByDescending(p => p.ViewCount),
            "use_count" => query.OrderByDescending(p => p.UseCount),
            _ => query.OrderByDescending(p => p.CreateTime)
        };

        // Apply pagination
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var result = items.Select(p => new PromptListItem
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            FinalPrompt = p.FinalPrompt,
            Language = p.Language,
            Format = p.Format,
            PromptType = p.PromptType,
            SystemPrompt = p.SystemPrompt,
            ConversationHistory = p.ConversationHistory,
            IsFavorite = p.IsFavorite,
            IsPublic = p.IsPublic,
            ViewCount = p.ViewCount,
            UseCount = p.UseCount,
            Tags = ParseTags(p.Tags),
            CurrentVersion = p.CurrentVersion,
            TotalVersions = p.TotalVersions,
            LastVersionTime = p.LastVersionTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            CreateTime = p.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdateTime = p.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();

        return new PromptListData
        {
            Total = total,
            Page = page,
            Limit = limit,
            Items = result
        };
    }

    public async Task<PromptInfo?> GetPromptDetailAsync(int userId, int promptId)
    {
        var prompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (prompt == null)
        {
            _logger.LogWarning("⚠️  提示词不存在或无权限: prompt_id={PromptId}, user_id={UserId}", promptId, userId);
            return null;
        }

        _logger.LogDebug("✅ 查询提示词详情成功: prompt_id={PromptId}, user_id={UserId}", promptId, userId);

        return new PromptInfo
        {
            Id = prompt.Id,
            UserId = prompt.UserId,
            Title = prompt.Title,
            Description = prompt.Description,
            RequirementReport = prompt.RequirementReport,
            ThinkingPoints = ParseJsonArray(prompt.ThinkingPoints),
            InitialPrompt = prompt.InitialPrompt,
            Advice = ParseJsonArray(prompt.Advice),
            FinalPrompt = prompt.FinalPrompt,
            Language = prompt.Language,
            Format = prompt.Format,
            PromptType = prompt.PromptType,
            SystemPrompt = prompt.SystemPrompt,
            ConversationHistory = prompt.ConversationHistory,
            IsFavorite = prompt.IsFavorite,
            IsPublic = prompt.IsPublic,
            ViewCount = prompt.ViewCount,
            UseCount = prompt.UseCount,
            Tags = ParseTags(prompt.Tags),
            CurrentVersion = prompt.CurrentVersion,
            TotalVersions = prompt.TotalVersions,
            LastVersionTime = prompt.LastVersionTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            CreateTime = prompt.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdateTime = prompt.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<bool> UpdatePromptAsync(int userId, int promptId, SavePromptRequest data)
    {
        var prompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (prompt == null)
        {
            _logger.LogWarning("⚠️  无权限更新提示词: prompt_id={PromptId}, user_id={UserId}", promptId, userId);
            return false;
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(data.Title))
            prompt.Title = data.Title;

        if (data.Description != null)
            prompt.Description = data.Description;

        if (data.RequirementReport != null)
            prompt.RequirementReport = data.RequirementReport;

        if (data.ThinkingPoints != null)
            prompt.ThinkingPoints = JsonSerializer.Serialize(data.ThinkingPoints);

        if (data.InitialPrompt != null)
            prompt.InitialPrompt = data.InitialPrompt;

        if (data.Advice != null)
            prompt.Advice = JsonSerializer.Serialize(data.Advice);

        if (data.FinalPrompt != null)
            prompt.FinalPrompt = data.FinalPrompt;

        if (!string.IsNullOrEmpty(data.Language))
            prompt.Language = data.Language;

        if (!string.IsNullOrEmpty(data.Format))
            prompt.Format = data.Format;

        if (!string.IsNullOrEmpty(data.PromptType))
            prompt.PromptType = data.PromptType;

        if (data.SystemPrompt != null)
            prompt.SystemPrompt = data.SystemPrompt;

        if (data.ConversationHistory != null)
            prompt.ConversationHistory = data.ConversationHistory;

        if (data.Tags != null)
        {
            prompt.Tags = string.Join(",", data.Tags);
            await UpdateTagsAsync(userId, data.Tags);
        }

        prompt.UpdateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("✅ 更新提示词成功: prompt_id={PromptId}, user_id={UserId}", promptId, userId);

        return true;
    }

    public async Task<bool> DeletePromptAsync(int userId, int promptId)
    {
        var prompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (prompt == null)
        {
            _logger.LogWarning("⚠️  无权限删除提示词: prompt_id={PromptId}, user_id={UserId}", promptId, userId);
            return false;
        }

        _context.Prompts.Remove(prompt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 删除提示词成功: prompt_id={PromptId}, user_id={UserId}", promptId, userId);
        return true;
    }

    public async Task<bool> ToggleFavoriteAsync(int userId, int promptId, bool isFavorite)
    {
        var prompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (prompt == null)
        {
            _logger.LogWarning("⚠️  无权限操作提示词: prompt_id={PromptId}, user_id={UserId}", promptId, userId);
            return false;
        }

        prompt.IsFavorite = isFavorite ? 1 : 0;
        await _context.SaveChangesAsync();

        var action = isFavorite ? "收藏" : "取消收藏";
        _logger.LogInformation("✅ {Action}提示词成功: prompt_id={PromptId}, user_id={UserId}", action, promptId, userId);
        return true;
    }

    public async Task IncreaseViewCountAsync(int promptId)
    {
        var prompt = await _context.Prompts.FindAsync(promptId);
        if (prompt != null)
        {
            prompt.ViewCount++;
            await _context.SaveChangesAsync();
            _logger.LogDebug("✅ 增加查看次数: prompt_id={PromptId}", promptId);
        }
    }

    public async Task<bool> IncreaseUseCountAsync(int userId, int promptId)
    {
        var prompt = await _context.Prompts
            .FirstOrDefaultAsync(p => p.Id == promptId && p.UserId == userId);

        if (prompt == null)
            return false;

        prompt.UseCount++;
        await _context.SaveChangesAsync();
        _logger.LogDebug("✅ 增加使用次数: prompt_id={PromptId}", promptId);
        return true;
    }

    private async Task UpdateTagsAsync(int userId, List<string> tags)
    {
        foreach (var tagName in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var existingTag = await _context.PromptTags
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TagName == tagName);

            if (existingTag != null)
            {
                existingTag.UseCount++;
            }
            else
            {
                _context.PromptTags.Add(new PromptTag
                {
                    TagName = tagName,
                    UserId = userId,
                    UseCount = 1
                });
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogDebug("✅ 更新标签统计成功: user_id={UserId}, tags={Tags}", userId, string.Join(",", tags));
    }

    private static List<string> ParseTags(string? tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr))
            return new List<string>();

        // Check if it's JSON array format
        if (tagsStr.StartsWith("[") && tagsStr.EndsWith("]"))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(tagsStr) ?? new List<string>();
            }
            catch
            {
                // Fall through to comma-separated parsing
            }
        }

        // Comma-separated format
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
