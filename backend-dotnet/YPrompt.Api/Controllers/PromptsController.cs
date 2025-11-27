using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Services;

namespace YPrompt.Api.Controllers;

[ApiController]
[Route("api/prompts")]
[Authorize]
public class PromptsController : ControllerBase
{
    private readonly IPromptService _promptService;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IPromptService promptService, ILogger<PromptsController> logger)
    {
        _promptService = promptService;
        _logger = logger;
    }

    /// <summary>
    /// 保存提示词(统一接口)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavePromptData>>> SavePrompt([FromBody] SavePromptRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<SavePromptData>.Error(401, "未授权"));
            }

            if (string.IsNullOrEmpty(request.Title))
            {
                return Ok(ApiResponse<SavePromptData>.Error(400, "标题不能为空"));
            }

            if (string.IsNullOrEmpty(request.FinalPrompt))
            {
                return Ok(ApiResponse<SavePromptData>.Error(400, "最终提示词不能为空"));
            }

            var result = await _promptService.SavePromptAsync(userId.Value, request);

            return Ok(ApiResponse<SavePromptData>.Success(result, result.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("⚠️  权限错误: {Message}", ex.Message);
            return Ok(ApiResponse<SavePromptData>.Error(403, ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("⚠️  参数错误: {Message}", ex.Message);
            return Ok(ApiResponse<SavePromptData>.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 保存提示词失败: {Message}", ex.Message);
            return Ok(ApiResponse<SavePromptData>.Error(500, $"保存失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取提示词列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PromptListData>>> GetPromptsList(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? is_favorite = null,
        [FromQuery] string sort = "create_time")
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<PromptListData>.Error(401, "未授权"));
            }

            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var result = await _promptService.GetPromptsListAsync(
                userId.Value, page, limit, keyword, tag, is_favorite, sort);

            return Ok(ApiResponse<PromptListData>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 查询提示词列表失败: {Message}", ex.Message);
            return Ok(ApiResponse<PromptListData>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取提示词详情
    /// </summary>
    [HttpGet("{promptId:int}")]
    public async Task<ActionResult<ApiResponse<PromptInfo>>> GetPromptDetail(int promptId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<PromptInfo>.Error(401, "未授权"));
            }

            var prompt = await _promptService.GetPromptDetailAsync(userId.Value, promptId);

            if (prompt == null)
            {
                return Ok(ApiResponse<PromptInfo>.Error(404, "提示词不存在或无权限访问"));
            }

            // Increase view count
            await _promptService.IncreaseViewCountAsync(promptId);

            return Ok(ApiResponse<PromptInfo>.Success(prompt));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 查询提示词详情失败: {Message}", ex.Message);
            return Ok(ApiResponse<PromptInfo>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 更新提示词
    /// </summary>
    [HttpPut("{promptId:int}")]
    public async Task<ActionResult<ApiResponse>> UpdatePrompt(int promptId, [FromBody] SavePromptRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            var success = await _promptService.UpdatePromptAsync(userId.Value, promptId, request);

            if (!success)
            {
                return Ok(ApiResponse.Error(403, "无权限修改或提示词不存在"));
            }

            return Ok(ApiResponse.Success("更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 更新提示词失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"更新失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除提示词
    /// </summary>
    [HttpDelete("{promptId:int}")]
    public async Task<ActionResult<ApiResponse>> DeletePrompt(int promptId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            var success = await _promptService.DeletePromptAsync(userId.Value, promptId);

            if (!success)
            {
                return Ok(ApiResponse.Error(403, "无权限删除或提示词不存在"));
            }

            return Ok(ApiResponse.Success("删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 删除提示词失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"删除失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 收藏/取消收藏提示词
    /// </summary>
    [HttpPost("{promptId:int}/favorite")]
    public async Task<ActionResult<ApiResponse>> ToggleFavorite(int promptId, [FromBody] FavoriteRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            var success = await _promptService.ToggleFavoriteAsync(userId.Value, promptId, request.IsFavorite);

            if (!success)
            {
                return Ok(ApiResponse.Error(403, "操作失败或提示词不存在"));
            }

            return Ok(ApiResponse.Success("操作成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 操作收藏状态失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"操作失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 记录提示词使用
    /// </summary>
    [HttpPost("{promptId:int}/use")]
    public async Task<ActionResult<ApiResponse>> RecordUse(int promptId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            var success = await _promptService.IncreaseUseCountAsync(userId.Value, promptId);

            if (!success)
            {
                return Ok(ApiResponse.Error(404, "提示词不存在"));
            }

            return Ok(ApiResponse.Success("记录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 记录使用次数失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"记录失败: {ex.Message}"));
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
