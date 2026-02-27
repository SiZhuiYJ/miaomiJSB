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
/// 打卡计划管理接口，提供完整的计划生命周期管理功能。
/// 支持创建、查询、更新、删除打卡计划，以及时间段配置管理。
/// 实现计划的启用/禁用控制，支持灵活的时间段设置和排序。
/// </summary>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class PlansController(DailyCheckDbContext db) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;

    /// <summary>
    /// 获取当前登录用户的打卡计划列表接口。
    /// 返回用户所有未删除的打卡计划，按开始日期升序排列。
    /// 每个计划包含关联的时间段配置信息。
    /// </summary>
    /// <returns>
    /// 成功时返回200 OK，包含计划概要列表；
    /// 每个计划项包含基本属性和时间段详情。
    /// </returns>
    /// <response code="200">获取计划列表成功</response>
    [HttpGet]
    public async Task<ActionResult<List<PlanSummary>>> GetMyPlans()
    {
        var userId = GetUserId();
        var plans = await _db.CheckinPlans
            .Include(x => x.CheckinPlanTimeSlots)
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderBy(x => x.StartDate)
            .Select(x => new PlanSummary
            {
                Id = (long)x.Id,
                CheckinMode = x.CheckinMode,
                Title = x.Title,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive,
                TimeSlots = x.CheckinPlanTimeSlots
                    .Where(ts => ts.IsActive == true)
                    .OrderBy(ts => ts.OrderNum)
                    .ThenBy(ts => ts.StartTime)
                    .Select(ts => new TimeSlotDto
                    {
                        Id = ts.Id,
                        SlotName = ts.SlotName,
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime,
                        OrderNum = ts.OrderNum,
                        IsActive = ts.IsActive ?? true
                    }).ToList()
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// 创建新的打卡计划接口。
    /// 支持设置计划标题、描述、时间范围和时间段配置。
    /// 自动验证时间段的有效性和无重叠性。
    /// 新创建的计划默认处于启用状态。
    /// </summary>
    /// <param name="request">创建计划请求参数，包含计划基本信息和时间段配置。</param>
    /// <returns>
    /// 成功时返回201 Created，包含新创建的计划概要信息；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="201">计划创建成功</response>
    /// <response code="400">请求参数错误，如时间段设置无效</response>
    [HttpPost]
    public async Task<ActionResult<PlanSummary>> CreatePlan(CreatePlanRequest request)
    {
        var userId = GetUserId();
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // 验证时间段
        if (request.TimeSlots != null && request.TimeSlots.Count > 0)
        {
            foreach (var slot in request.TimeSlots)
            {
                if (slot.StartTime >= slot.EndTime)
                {
                    return BadRequest(new { message = $"时间段 {slot.SlotName} 开始时间必须早于结束时间" });
                }
            }

            var sortedSlots = request.TimeSlots.OrderBy(s => s.StartTime).ToList();
            for (int i = 0; i < sortedSlots.Count - 1; i++)
            {
                if (sortedSlots[i].EndTime > sortedSlots[i + 1].StartTime)
                {
                    return BadRequest(new { message = "时间段存在重叠，请检查设置" });
                }
            }
        }

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
            UpdatedAt = DateTime.UtcNow,
            CheckinMode = request.TimeSlots?.Count > 0 ? (byte)1 : (byte)0 // 根据是否有时间段配置设置打卡模式
        };

        if (request.TimeSlots != null)
        {
            foreach (var slotDto in request.TimeSlots)
            {
                plan.CheckinPlanTimeSlots.Add(new CheckinPlanTimeSlot
                {
                    SlotName = slotDto.SlotName,
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    OrderNum = slotDto.OrderNum,
                    IsActive = slotDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        _db.CheckinPlans.Add(plan);
        await _db.SaveChangesAsync();

        var result = new PlanSummary
        {
            Id = (long)plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            IsActive = plan.IsActive,
            CheckinMode = plan.CheckinMode,
            TimeSlots = [.. plan.CheckinPlanTimeSlots.Select(ts => new TimeSlotDto
            {
                Id = ts.Id,
                SlotName = ts.SlotName,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                OrderNum = ts.OrderNum,
                IsActive = ts.IsActive ?? true
            })]
        };

        return CreatedAtAction(nameof(GetMyPlans), new { id = plan.Id }, result);
    }

    /// <summary>
    /// 更新指定打卡计划接口。
    /// 支持更新计划的基本信息和时间段配置。
    /// 时间段更新采用全量替换策略，会删除原有配置并重建。
    /// 自动验证时间段的有效性和无重叠性。
    /// </summary>
    /// <param name="request">更新计划请求参数，包含计划ID和需要更新的属性。</param>
    /// <returns>
    /// 成功时返回204 No Content；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="204">计划更新成功</response>
    /// <response code="400">请求参数错误或时间段设置无效</response>
    /// <response code="404">计划不存在或无权限访问</response>
    [HttpPost]
    [Route("update")]
    public async Task<ActionResult> UpdatePlan(UpdatePlanRequest request)
    {
        var userId = GetUserId();
        var plan = await _db.CheckinPlans
            .Include(x => x.CheckinPlanTimeSlots)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
        {
            return NotFound(new { message = "未找到计划" });
        }

        // 验证时间段
        if (request.TimeSlots != null)
        {
            if (request.TimeSlots.Count > 0)
            {
                foreach (var slot in request.TimeSlots)
                {
                    if (slot.StartTime >= slot.EndTime)
                    {
                        return BadRequest(new { message = $"时间段 {slot.SlotName} 开始时间必须早于结束时间" });
                    }
                }

                var sortedSlots = request.TimeSlots.OrderBy(s => s.StartTime).ToList();
                for (int i = 0; i < sortedSlots.Count - 1; i++)
                {
                    if (sortedSlots[i].EndTime > sortedSlots[i + 1].StartTime)
                    {
                        return BadRequest(new { message = "时间段存在重叠，请检查设置" });
                    }
                }
            }

            // 更新时间段：先清除旧的（或者标记删除），这里选择物理删除重建，简单有效
            _db.CheckinPlanTimeSlots.RemoveRange(plan.CheckinPlanTimeSlots);

            foreach (var slotDto in request.TimeSlots)
            {
                plan.CheckinPlanTimeSlots.Add(new CheckinPlanTimeSlot
                {
                    PlanId = plan.Id,
                    SlotName = slotDto.SlotName,
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    OrderNum = slotDto.OrderNum,
                    IsActive = slotDto.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
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
    /// 删除指定打卡计划接口。
    /// 采用软删除机制，仅标记计划为已删除状态。
    /// 不会影响已存在的打卡记录，但计划将不再显示在列表中。
    /// </summary>
    /// <param name="PlanId">要删除的打卡计划ID。</param>
    /// <returns>
    /// 成功时返回204 No Content；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="204">计划删除成功</response>
    /// <response code="404">计划不存在或无权限访问</response>
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
