using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Services;

namespace YPrompt.Api.Controllers;

[ApiController]
[Route("api/prompt-rules")]
[Authorize]
public class PromptRulesController : ControllerBase
{
    private readonly IPromptRulesService _promptRulesService;
    private readonly ILogger<PromptRulesController> _logger;

    public PromptRulesController(IPromptRulesService promptRulesService, ILogger<PromptRulesController> logger)
    {
        _promptRulesService = promptRulesService;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的提示词规则
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PromptRulesData>>> GetRules()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<PromptRulesData>.Error(401, "未授权"));
            }

            var rules = await _promptRulesService.GetUserRulesAsync(userId.Value);

            if (rules == null)
            {
                return Ok(new ApiResponse<PromptRulesData>
                {
                    Code = 200,
                    Data = null,
                    Message = "用户暂无自定义规则"
                });
            }

            return Ok(ApiResponse<PromptRulesData>.Success(rules));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 获取提示词规则失败: {Message}", ex.Message);
            return Ok(ApiResponse<PromptRulesData>.Error(500, $"获取提示词规则失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 保存用户的提示词规则
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PromptRulesData>>> SaveRules([FromBody] SavePromptRulesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<PromptRulesData>.Error(401, "未授权"));
            }

            var savedRules = await _promptRulesService.SaveUserRulesAsync(userId.Value, request);

            return Ok(ApiResponse<PromptRulesData>.Success(savedRules, "保存成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 保存提示词规则失败: {Message}", ex.Message);
            return Ok(ApiResponse<PromptRulesData>.Error(500, $"保存提示词规则失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除用户的提示词规则（重置为默认）
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse>> DeleteRules()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            await _promptRulesService.DeleteUserRulesAsync(userId.Value);

            return Ok(ApiResponse.Success("已重置为默认规则"));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 删除提示词规则失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"删除提示词规则失败: {ex.Message}"));
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
