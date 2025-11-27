using Microsoft.EntityFrameworkCore;
using YPrompt.Api.Data;
using YPrompt.Api.Models.DTOs;
using YPrompt.Api.Models.Entities;

namespace YPrompt.Api.Services;

public interface IPromptRulesService
{
    Task<PromptRulesData?> GetUserRulesAsync(int userId);
    Task<PromptRulesData> SaveUserRulesAsync(int userId, SavePromptRulesRequest data);
    Task DeleteUserRulesAsync(int userId);
}

public class PromptRulesService : IPromptRulesService
{
    private readonly YPromptDbContext _context;
    private readonly ILogger<PromptRulesService> _logger;

    public PromptRulesService(YPromptDbContext context, ILogger<PromptRulesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PromptRulesData?> GetUserRulesAsync(int userId)
    {
        var rules = await _context.UserPromptRules
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (rules == null)
            return null;

        return MapToDto(rules);
    }

    public async Task<PromptRulesData> SaveUserRulesAsync(int userId, SavePromptRulesRequest data)
    {
        var existingRules = await _context.UserPromptRules
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (existingRules != null)
        {
            // Update existing rules
            UpdateRulesFromRequest(existingRules, data);
            existingRules.UpdateTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 更新用户提示词规则成功: user_id={UserId}", userId);

            return MapToDto(existingRules);
        }
        else
        {
            // Create new rules
            var newRules = new UserPromptRules
            {
                UserId = userId
            };
            UpdateRulesFromRequest(newRules, data);

            _context.UserPromptRules.Add(newRules);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ 创建用户提示词规则成功: user_id={UserId}", userId);

            return MapToDto(newRules);
        }
    }

    public async Task DeleteUserRulesAsync(int userId)
    {
        var rules = await _context.UserPromptRules
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (rules != null)
        {
            _context.UserPromptRules.Remove(rules);
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 删除用户提示词规则成功: user_id={UserId}", userId);
        }
    }

    private static void UpdateRulesFromRequest(UserPromptRules rules, SavePromptRulesRequest data)
    {
        if (data.SystemPromptRules != null)
            rules.SystemPromptRules = data.SystemPromptRules;

        if (data.UserGuidedPromptRules != null)
            rules.UserGuidedPromptRules = data.UserGuidedPromptRules;

        if (data.RequirementReportRules != null)
            rules.RequirementReportRules = data.RequirementReportRules;

        if (data.ThinkingPointsExtractionPrompt != null)
            rules.ThinkingPointsExtractionPrompt = data.ThinkingPointsExtractionPrompt;

        if (data.ThinkingPointsSystemMessage != null)
            rules.ThinkingPointsSystemMessage = data.ThinkingPointsSystemMessage;

        if (data.SystemPromptGenerationPrompt != null)
            rules.SystemPromptGenerationPrompt = data.SystemPromptGenerationPrompt;

        if (data.SystemPromptSystemMessage != null)
            rules.SystemPromptSystemMessage = data.SystemPromptSystemMessage;

        if (data.OptimizationAdvicePrompt != null)
            rules.OptimizationAdvicePrompt = data.OptimizationAdvicePrompt;

        if (data.OptimizationAdviceSystemMessage != null)
            rules.OptimizationAdviceSystemMessage = data.OptimizationAdviceSystemMessage;

        if (data.OptimizationApplicationPrompt != null)
            rules.OptimizationApplicationPrompt = data.OptimizationApplicationPrompt;

        if (data.OptimizationApplicationSystemMessage != null)
            rules.OptimizationApplicationSystemMessage = data.OptimizationApplicationSystemMessage;

        if (data.QualityAnalysisSystemPrompt != null)
            rules.QualityAnalysisSystemPrompt = data.QualityAnalysisSystemPrompt;

        if (data.UserPromptQualityAnalysis != null)
            rules.UserPromptQualityAnalysis = data.UserPromptQualityAnalysis;

        if (data.UserPromptQuickOptimization != null)
            rules.UserPromptQuickOptimization = data.UserPromptQuickOptimization;

        if (data.UserPromptRulesContent != null)
            rules.UserPromptRulesContent = data.UserPromptRulesContent;
    }

    private static PromptRulesData MapToDto(UserPromptRules rules)
    {
        return new PromptRulesData
        {
            Id = rules.Id,
            UserId = rules.UserId,
            SystemPromptRules = rules.SystemPromptRules,
            UserGuidedPromptRules = rules.UserGuidedPromptRules,
            RequirementReportRules = rules.RequirementReportRules,
            ThinkingPointsExtractionPrompt = rules.ThinkingPointsExtractionPrompt,
            ThinkingPointsSystemMessage = rules.ThinkingPointsSystemMessage,
            SystemPromptGenerationPrompt = rules.SystemPromptGenerationPrompt,
            SystemPromptSystemMessage = rules.SystemPromptSystemMessage,
            OptimizationAdvicePrompt = rules.OptimizationAdvicePrompt,
            OptimizationAdviceSystemMessage = rules.OptimizationAdviceSystemMessage,
            OptimizationApplicationPrompt = rules.OptimizationApplicationPrompt,
            OptimizationApplicationSystemMessage = rules.OptimizationApplicationSystemMessage,
            QualityAnalysisSystemPrompt = rules.QualityAnalysisSystemPrompt,
            UserPromptQualityAnalysis = rules.UserPromptQualityAnalysis,
            UserPromptQuickOptimization = rules.UserPromptQuickOptimization,
            UserPromptRulesContent = rules.UserPromptRulesContent,
            CreateTime = rules.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdateTime = rules.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
