using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

/// <summary>
/// 打卡计划相关接口，包含查询和创建计划。
/// </summary>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class PlansController(DailyCheckDbContext db) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;

    /// <summary>
    /// 获取当前登录用户的打卡计划列表。
    /// </summary>
    /// <returns>当前用户的计划概要列表。</returns>
    [HttpGet]
    public async Task<ActionResult<List<PlanSummary>>> GetMyPlans()
    {
        var userId = GetUserId();
        var plans = await _db.CheckinPlans
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderBy(x => x.StartDate)
            .Select(x => new PlanSummary
            {
                Id = (long)x.Id,
                Title = x.Title,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// 为当前登录用户创建新的打卡计划。
    /// </summary>
    /// <param name="request">创建计划的请求参数。</param>
    /// <returns>新创建的计划概要信息。</returns>
    [HttpPost]
    public async Task<ActionResult<PlanSummary>> CreatePlan(CreatePlanRequest request)
    {
        var userId = GetUserId();
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var plan = new CheckinPlan
        {
            UserId = (ulong)userId,
            Title = request.Title,
            Description = request.Description,
            StartDate = startDate,
            EndDate = request.EndDate,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CheckinPlans.Add(plan);
        await _db.SaveChangesAsync();

        var result = new PlanSummary
        {
            Id = (long)plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            IsActive = plan.IsActive
        };

        return CreatedAtAction(nameof(GetMyPlans), new { id = plan.Id }, result);
    }

    /// <summary>
    /// 更新指定的打卡计划。
    /// </summary>
    /// <param name="request">更新计划的请求参数。</param>
    /// <returns></returns>
    [HttpPost]
    [Route("update")]
    public async Task<ActionResult> UpdatePlan(UpdatePlanRequest request)
    {
        var userId = GetUserId();
        var plan = await _db.CheckinPlans
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
        {
            return NotFound(new { message = "未找到计划" });
        }
        plan.Title = request.Title ?? plan.Title;
        plan.Description = request.Description ?? plan.Description;
        plan.StartDate = request.StartDate ?? plan.StartDate;
        plan.EndDate = request.EndDate ?? plan.EndDate;
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 删除指定的打卡计划。
    /// </summary>
    /// <param name="PlanId">打卡计划ID</param>
    /// <returns></returns>
    [HttpPost]
    [Route("delete")]
    public async Task<ActionResult> DeletePlan(ulong PlanId)
    {
        var userId = GetUserId();
        var plan = await _db.CheckinPlans
            .FirstOrDefaultAsync(x => x.Id == PlanId && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
        {
            return NotFound(new { message = "未找到计划" });
        }
        plan.IsDeleted = true;
        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }


    /// <summary>
    /// 从当前访问令牌中解析用户 ID。
    /// </summary>
    /// <returns>当前登录用户的 ID。</returns>
    ulong GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var subClaim = User.FindFirstValue("sub");
        var value = idClaim ?? subClaim;
        return value == null ? throw new InvalidOperationException("User id not found in token") : ulong.Parse(value);
    }
}
