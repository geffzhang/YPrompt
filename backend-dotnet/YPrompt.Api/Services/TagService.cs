using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Models.Entities;

namespace YPrompt.Api.Services;

public interface ITagService
{
    Task<List<TagInfo>> GetUserTagsAsync(int userId, int limit);
    Task<TagInfo> CreateTagAsync(int userId, string tagName);
    Task<bool> DeleteTagAsync(int userId, int tagId);
    Task<List<TagInfo>> GetPopularTagsAsync(int userId, int limit);
}

public class TagService : ITagService
{
    private readonly YPromptDbContext _context;
    private readonly ILogger<TagService> _logger;

    public TagService(YPromptDbContext context, ILogger<TagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TagInfo>> GetUserTagsAsync(int userId, int limit)
    {
        var tags = await _context.PromptTags
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.UseCount)
            .ThenByDescending(t => t.CreateTime)
            .Take(limit)
            .Select(t => new TagInfo
            {
                Id = t.Id,
                TagName = t.TagName,
                UseCount = t.UseCount,
                CreateTime = t.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        _logger.LogDebug("✅ 查询用户标签成功: user_id={UserId}, count={Count}", userId, tags.Count);

        return tags;
    }

    public async Task<TagInfo> CreateTagAsync(int userId, string tagName)
    {
        // Check if tag already exists
        var existingTag = await _context.PromptTags
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TagName == tagName);

        if (existingTag != null)
        {
            _logger.LogInformation("⚠️  标签已存在: tag_name={TagName}, user_id={UserId}", tagName, userId);
            return new TagInfo
            {
                Id = existingTag.Id,
                TagName = existingTag.TagName,
                UseCount = existingTag.UseCount,
                CreateTime = existingTag.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        // Create new tag
        var newTag = new PromptTag
        {
            TagName = tagName,
            UserId = userId,
            UseCount = 0
        };

        _context.PromptTags.Add(newTag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 创建标签成功: tag_id={TagId}, tag_name={TagName}, user_id={UserId}",
            newTag.Id, tagName, userId);

        return new TagInfo
        {
            Id = newTag.Id,
            TagName = newTag.TagName,
            UseCount = newTag.UseCount,
            CreateTime = newTag.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public async Task<bool> DeleteTagAsync(int userId, int tagId)
    {
        var tag = await _context.PromptTags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
        {
            _logger.LogWarning("⚠️  无权限删除标签: tag_id={TagId}, user_id={UserId}", tagId, userId);
            return false;
        }

        _context.PromptTags.Remove(tag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 删除标签成功: tag_id={TagId}, user_id={UserId}", tagId, userId);
        return true;
    }

    public async Task<List<TagInfo>> GetPopularTagsAsync(int userId, int limit)
    {
        var tags = await _context.PromptTags
            .Where(t => t.UserId == userId && t.UseCount > 0)
            .OrderByDescending(t => t.UseCount)
            .Take(limit)
            .Select(t => new TagInfo
            {
                Id = t.Id,
                TagName = t.TagName,
                UseCount = t.UseCount,
                CreateTime = t.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        _logger.LogDebug("✅ 查询热门标签成功: user_id={UserId}, count={Count}", userId, tags.Count);

        return tags;
    }
}
