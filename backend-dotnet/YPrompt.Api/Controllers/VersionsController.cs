using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Services;

namespace YPrompt.Api.Controllers;

[ApiController]
[Route("api/versions")]
[Authorize]
public class VersionsController : ControllerBase
{
    private readonly IVersionService _versionService;
    private readonly ILogger<VersionsController> _logger;

    public VersionsController(IVersionService versionService, ILogger<VersionsController> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    /// <summary>
    /// 创建新版本
    /// </summary>
    [HttpPost("{promptId:int}")]
    public async Task<ActionResult<ApiResponse<CreateVersionData>>> CreateVersion(
        int promptId,
        [FromBody] CreateVersionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<CreateVersionData>.Error(401, "未授权"));
            }

            if (string.IsNullOrEmpty(request.ChangeType))
            {
                return Ok(ApiResponse<CreateVersionData>.Error(400, "缺少change_type参数"));
            }

            if (request.ChangeType != "major" && request.ChangeType != "minor" && request.ChangeType != "patch")
            {
                return Ok(ApiResponse<CreateVersionData>.Error(400, "change_type必须是major、minor或patch"));
            }

            if (string.IsNullOrEmpty(request.ChangeSummary))
            {
                return Ok(ApiResponse<CreateVersionData>.Error(400, "缺少change_summary参数"));
            }

            var result = await _versionService.CreateVersionAsync(promptId, userId.Value, request);

            return Ok(ApiResponse<CreateVersionData>.Success(result, "版本创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<CreateVersionData>.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 创建版本失败: {Message}", ex.Message);
            return Ok(ApiResponse<CreateVersionData>.Error(500, $"创建失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取版本列表
    /// </summary>
    [HttpGet("{promptId:int}/versions")]
    public async Task<ActionResult<ApiResponse<VersionListData>>> GetVersionList(
        int promptId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? tag = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<VersionListData>.Error(401, "未授权"));
            }

            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 20;

            var result = await _versionService.GetVersionHistoryAsync(promptId, userId.Value, page, limit, tag);

            return Ok(ApiResponse<VersionListData>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<VersionListData>.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 获取版本列表失败: {Message}", ex.Message);
            return Ok(ApiResponse<VersionListData>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取版本详情
    /// </summary>
    [HttpGet("{promptId:int}/versions/{versionId:int}")]
    public async Task<ActionResult<ApiResponse<VersionInfo>>> GetVersionDetail(int promptId, int versionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<VersionInfo>.Error(401, "未授权"));
            }

            var version = await _versionService.GetVersionDetailAsync(promptId, userId.Value, versionId);

            return Ok(ApiResponse<VersionInfo>.Success(version));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<VersionInfo>.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 获取版本详情失败: {Message}", ex.Message);
            return Ok(ApiResponse<VersionInfo>.Error(500, $"查询失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 版本对比
    /// </summary>
    [HttpGet("{promptId:int}/versions/compare")]
    public async Task<ActionResult<ApiResponse<VersionCompareData>>> CompareVersions(
        int promptId,
        [FromQuery(Name = "from")] int? fromVersionId,
        [FromQuery(Name = "to")] int? toVersionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<VersionCompareData>.Error(401, "未授权"));
            }

            if (!fromVersionId.HasValue || !toVersionId.HasValue)
            {
                return Ok(ApiResponse<VersionCompareData>.Error(400, "缺少from或to参数"));
            }

            var result = await _versionService.CompareVersionsAsync(
                promptId, userId.Value, fromVersionId.Value, toVersionId.Value);

            return Ok(ApiResponse<VersionCompareData>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<VersionCompareData>.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 版本对比失败: {Message}", ex.Message);
            return Ok(ApiResponse<VersionCompareData>.Error(500, $"对比失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 回滚到指定版本
    /// </summary>
    [HttpPost("{promptId:int}/versions/{versionId:int}/rollback")]
    public async Task<ActionResult<ApiResponse<RollbackData>>> RollbackVersion(
        int promptId,
        int versionId,
        [FromBody] RollbackRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse<RollbackData>.Error(401, "未授权"));
            }

            var result = await _versionService.RollbackToVersionAsync(
                promptId, userId.Value, versionId, request?.ChangeSummary);

            return Ok(ApiResponse<RollbackData>.Success(result, "回滚成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<RollbackData>.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 回滚失败: {Message}", ex.Message);
            return Ok(ApiResponse<RollbackData>.Error(500, $"回滚失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 更新版本标签
    /// </summary>
    [HttpPut("{promptId:int}/versions/{versionId:int}/tag")]
    public async Task<ActionResult<ApiResponse>> UpdateVersionTag(
        int promptId,
        int versionId,
        [FromBody] UpdateTagRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            if (string.IsNullOrEmpty(request.VersionTag))
            {
                return Ok(ApiResponse.Error(400, "缺少version_tag参数"));
            }

            await _versionService.UpdateVersionTagAsync(promptId, userId.Value, versionId, request.VersionTag);

            return Ok(ApiResponse.Success("标签更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(404, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 更新标签失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"更新失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除版本
    /// </summary>
    [HttpDelete("{promptId:int}/versions/{versionId:int}")]
    public async Task<ActionResult<ApiResponse>> DeleteVersion(int promptId, int versionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Ok(ApiResponse.Error(401, "未授权"));
            }

            await _versionService.DeleteVersionAsync(promptId, userId.Value, versionId);

            return Ok(ApiResponse.Success("版本删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse.Error(400, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ 删除版本失败: {Message}", ex.Message);
            return Ok(ApiResponse.Error(500, $"删除失败: {ex.Message}"));
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
