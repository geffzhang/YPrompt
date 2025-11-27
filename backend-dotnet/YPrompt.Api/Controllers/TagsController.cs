using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Services;

namespace YPrompt.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户标签列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TagInfo>>>> GetTags([FromQuery] int limit = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<List<TagInfo>>.Error(401, "未授权"));
            }

            if (limit < 1 || limit > 100) limit = 50;

            var tags = await _tagService.GetUserTagsAsync(userId.Value, limit);

            return Ok(ApiResponse<List<TagInfo>>.Success(tags));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 查询标签列表失败: {Message}", ex.Message);
            return Ok(ApiResponse<List<TagInfo>>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 创建标签
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TagInfo>>> CreateTag([FromBody] CreateTagRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<TagInfo>.Error(401, "未授权"));
            }

            var tagName = request.TagName.Trim();

            if (string.IsNullOrEmpty(tagName))
            {
                return Ok(ApiResponse<TagInfo>.Error(400, "标签名称不能为空"));
            }

            if (tagName.Length > 50)
            {
                return Ok(ApiResponse<TagInfo>.Error(400, "标签名称不能超过50个字符"));
            }

            var tag = await _tagService.CreateTagAsync(userId.Value, tagName);

            return Ok(ApiResponse<TagInfo>.Success(tag, "创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 创建标签失败: {Message}", ex.Message);
            return Ok(ApiResponse<TagInfo>.Error(500, $"创建失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除标签
    /// </summary>
    [HttpDelete("{tagId:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteTag(int tagId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            var success = await _tagService.DeleteTagAsync(userId.Value, tagId);

            if (!success)
            {
                return Ok(ApiResponse.Error(403, "无权限删除或标签不存在"));
            }

            return Ok(ApiResponse.Success("删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 删除标签失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"删除失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取热门标签
    /// </summary>
    [HttpGet("popular")]
    public async Task<ActionResult<ApiResponse<List<TagInfo>>>> GetPopularTags([FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<List<TagInfo>>.Error(401, "未授权"));
            }

            if (limit < 1 || limit > 50) limit = 20;

            var tags = await _tagService.GetPopularTagsAsync(userId.Value, limit);

            return Ok(ApiResponse<List<TagInfo>>.Success(tags));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 查询热门标签失败: {Message}", ex.Message);
            return Ok(ApiResponse<List<TagInfo>>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }
}
