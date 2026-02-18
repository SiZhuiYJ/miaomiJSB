using System;
using System.Security.Claims;
using System.Text.Json;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace api.Controllers;

/// <summary>
/// 打卡记录相关接口，包括当天打卡、补打卡和打卡日历。
/// </summary>
/// <remarks>
/// 初始化打卡控制器。
/// </remarks>
/// <param name="db">打卡系统数据库上下文。</param>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class CheckinsController(DailyCheckDbContext db) : ControllerBase
{

    /// <summary>
    /// 用户当天打卡接口。
    /// </summary>
    /// <param name="request">打卡请求参数。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("daily")]
    public async Task<ActionResult> Daily(DailyCheckinRequest request)
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var plan = await db.CheckinPlans
            .Include(p => p.CheckinPlanTimeSlots)
            .SingleOrDefaultAsync(x => x.Id == (ulong)request.PlanId && x.UserId == userId && !x.IsDeleted && x.IsActive == true);
        if (plan == null)
            return NotFound("Plan not found");

        if (plan.StartDate > today)
            return BadRequest("Plan has not started yet");

        // 分时段打卡逻辑
        CheckinPlanTimeSlot? timeSlot = null;
        if (request.TimeSlotId.HasValue)
        {
            timeSlot = plan.CheckinPlanTimeSlots.FirstOrDefault(ts => ts.Id == request.TimeSlotId.Value && ts.IsActive == true);
            if (timeSlot == null)
                return BadRequest("Invalid time slot");

            // 验证时间是否在范围内
            // 假设服务器时区与用户一致，或用户接受服务器时间。这里使用 DateTime.UtcNow + 8 (CST)
            var now = DateTime.UtcNow.AddHours(8);
            var currentTime = TimeOnly.FromDateTime(now);

            if (currentTime < timeSlot.StartTime)
                return BadRequest($"Time slot {timeSlot.SlotName} has not started yet ({timeSlot.StartTime})");
            if (currentTime > timeSlot.EndTime)
                return BadRequest($"Time slot {timeSlot.SlotName} has ended ({timeSlot.EndTime}), please use retro checkin");
        }
        else if (plan.CheckinPlanTimeSlots.Any(ts => ts.IsActive == true))
        {
            return BadRequest("Time slot is required for this plan");
        }

        var existing = await db.Checkins
            .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.CheckDate == today && x.TimeSlotId == request.TimeSlotId && !x.IsDeleted);
        if (existing != null)
            return Conflict("Already checked in for today" + (request.TimeSlotId.HasValue ? " in this time slot" : ""));

        if (request.ImageUrls == null || request.ImageUrls.Count < 1 || request.ImageUrls.Count > 3)
            return BadRequest("Daily checkin must include between 1 and 3 images");

        var checkin = new Checkin
        {
            PlanId = plan.Id,
            UserId = userId,
            CheckDate = today,
            TimeSlotId = request.TimeSlotId,
            Images = request.ImageUrls != null && request.ImageUrls.Count > 0 ? JsonSerializer.Serialize(request.ImageUrls) : null,
            Note = request.Note,
            Status = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Checkins.Add(checkin);
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// 补打卡接口。
    /// </summary>
    /// <param name="request">补打卡请求参数。</param>
    /// <returns>操作结果。</returns>
    [HttpPost("retro")]
    public async Task<ActionResult> Retro(RetroCheckinRequest request)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow.AddHours(8);
        var today = DateOnly.FromDateTime(now.Date);

        if (request.Date > today)
            return BadRequest("Retro checkin date cannot be in the future");

        var plan = await db.CheckinPlans
            .Include(p => p.CheckinPlanTimeSlots)
            .SingleOrDefaultAsync(x => x.Id == (ulong)request.PlanId && x.UserId == userId && !x.IsDeleted && x.IsActive == true);
        if (plan == null)
            return NotFound("Plan not found");

        if (plan.StartDate > request.Date)
            return BadRequest("Plan was not active on that date");

        // 分时段逻辑
        if (request.TimeSlotId.HasValue)
        {
            var slot = plan.CheckinPlanTimeSlots.FirstOrDefault(ts => ts.Id == request.TimeSlotId.Value && ts.IsActive == true);
            if (slot == null)
                return BadRequest("Invalid time slot");

            if (request.Date == today)
            {
                var slotEnd = request.Date.ToDateTime(slot.EndTime);
                if (now <= slotEnd)
                    return BadRequest($"Current time is within time slot {slot.SlotName}, please use daily checkin");
            }
        }
        else if (plan.CheckinPlanTimeSlots.Any(ts => ts.IsActive == true))
        {
            return BadRequest("Time slot is required for this plan");
        }
        else if (request.Date == today)
        {
            var dayEnd = request.Date.ToDateTime(new TimeOnly(23, 59, 59));
            if (now <= dayEnd)
                return BadRequest("Current time is within checkin day, please use daily checkin");
        }

        var existing = await db.Checkins
            .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.CheckDate == request.Date && x.TimeSlotId == request.TimeSlotId && !x.IsDeleted);
        if (existing != null)
            return Conflict("Already checked in for that date" + (request.TimeSlotId.HasValue ? " in this time slot" : ""));

        if (request.ImageUrls == null || request.ImageUrls.Count < 1 || request.ImageUrls.Count > 3)
            return BadRequest("Retro checkin must include between 1 and 3 images");

        var checkin = new Checkin
        {
            PlanId = plan.Id,
            UserId = userId,
            CheckDate = request.Date,
            TimeSlotId = request.TimeSlotId,
            Images = request.ImageUrls != null && request.ImageUrls.Count > 0 ? JsonSerializer.Serialize(request.ImageUrls) : null,
            Note = request.Note,
            Status = 2,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Checkins.Add(checkin);
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// 获取指定打卡计划在某年某月的日历打卡状态。
    /// </summary>
    /// <param name="planId">打卡计划 ID。</param>
    /// <param name="year">年份。</param>
    /// <param name="month">月份。</param>
    /// <returns>日历打卡状态列表。</returns>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<CalendarStatusItem>>> GetCalendar(long planId, int year, int month)
    {
        var userId = GetUserId();
        var plan = await db.CheckinPlans.SingleOrDefaultAsync(x => x.Id == (ulong)planId && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
            return NotFound("Plan not found");

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var checkins = await db.Checkins
            .Where(x => x.PlanId == (ulong)planId && x.CheckDate >= firstDay && x.CheckDate <= lastDay && !x.IsDeleted)
            .ToListAsync();

        var activeSlotsCount = plan.CheckinPlanTimeSlots.Count(x => x.IsActive == true);
        var result = new List<CalendarStatusItem>();
        var grouped = checkins.GroupBy(x => x.CheckDate);
        if (plan.CheckinMode == 0)
        {

            foreach (var g in grouped)
            {
                var date = g.Key;
                sbyte status = 1;
                bool isComplete = false;

                if (activeSlotsCount == 0)
                {
                    isComplete = true;
                    status = g.Max(x => x.Status);
                }
                else
                {
                    var checkedSlots = g.Select(x => x.TimeSlotId).Where(x => x.HasValue).Distinct().Count();
                    if (checkedSlots >= activeSlotsCount)
                    {
                        isComplete = true;
                        // If any is retro (2), the day status is 2 (Retro/Yellow)
                        if (g.Any(x => x.Status == 2)) status = 2;
                        else status = 1;
                    }
                }

                if (isComplete)
                {
                    result.Add(new CalendarStatusItem { Date = date, Status = status });
                }
            }
        }
        else if (plan.CheckinMode == 1)
        {
            // 获取该计划的所有时间段
            var allSlots = await db.CheckinPlanTimeSlots
                .Where(ts => ts.PlanId == plan.Id && ts.IsActive == true)
                .ToListAsync();

            // 按日期分组处理每段时间的打卡情况
            var startDate = plan.StartDate;
            var currentDate = firstDay;

            // 遍历该月的每一天
            while (currentDate <= lastDay)
            {
                var dayCheckins = grouped.FirstOrDefault(g => g.Key == currentDate);

                // 获取当天所有时间段的打卡状态
                var timeSlotStatuses = new List<TimeSlotStatusItem>();

                foreach (var slot in allSlots)
                {
                    var slotCheckin = dayCheckins?.FirstOrDefault(c => c.TimeSlotId == slot.Id);

                    if (slotCheckin != null)
                    {
                        timeSlotStatuses.Add(new TimeSlotStatusItem
                        {
                            CheckinId = (int)slotCheckin.Id,
                            TimeSlotId = slot.Id,
                            Status = slotCheckin.Status
                        });
                    }
                }

                // 判断当天是否完成全部打卡
                var completedSlots = timeSlotStatuses.Count(t => t.Status > 0);
                if (completedSlots > 0)
                {
                    var totalSlots = allSlots.Count;

                    sbyte overallStatus = 0; // 默认未完成
                    if (completedSlots == totalSlots)
                    {
                        // 全部完成，取最高状态（如果有补打卡则显示补打卡状态）
                        overallStatus = (sbyte)(timeSlotStatuses.Any(t => t.Status == 2) ? 2 : 1);
                    }
                    else if (completedSlots > 0)
                    {
                        // 部分完成
                        overallStatus = (sbyte)(timeSlotStatuses.Any(t => t.Status == 2) ? 2 : 1);
                    }

                    result.Add(new CalendarStatusItem
                    {
                        Date = currentDate,
                        Status = overallStatus,
                        CheckinMode = plan.CheckinMode,
                        TimeSlots = timeSlotStatuses
                    });
                }
                currentDate = currentDate.AddDays(1);
            }

        }
        return Ok(result);
    }

    /// <summary>
    /// 获取某个打卡计划在指定日期的打卡详情。
    /// </summary>
    /// <param name="planId">打卡计划 ID。</param>
    /// <param name="date">需要查询的日期。</param>
    /// <returns>打卡详情。</returns>
    [HttpGet("detail")]
    public async Task<ActionResult<List<CheckinDetailResponse>>> GetDetail(long planId, DateOnly date)
    {
        var userId = GetUserId();

        var plan = await db.CheckinPlans.SingleOrDefaultAsync(x => x.Id == (ulong)planId && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
            return NotFound("Plan not found");

        var checkins = await db.Checkins
            .Where(x => x.PlanId == plan.Id && x.CheckDate == date && !x.IsDeleted)
            .ToListAsync();

        if (checkins.Count == 0)
            return NotFound("Checkin not found"); // 或者返回空列表，视前端需求而定，这里保持原有一致性可能返回 404

        var result = checkins.Select(checkin =>
        {
            var images = new List<string>();
            if (!string.IsNullOrWhiteSpace(checkin.Images))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(checkin.Images);
                    if (parsed != null)
                        images = parsed;
                }
                catch
                {
                }
            }

            return new CheckinDetailResponse
            {
                Date = checkin.CheckDate,
                Status = checkin.Status,
                Note = checkin.Note,
                ImageUrls = images,
                TimeSlotId = checkin.TimeSlotId
            };
        }).ToList();

        return Ok(result); // 注意：这里返回类型变为了 List，前端需要适配
    }

    /// <summary>
    /// 从当前访问令牌中解析用户 ID。
    /// </summary>
    /// <returns>当前登录用户的 ID。</returns>
    ulong GetUserId()
    {
        var candidateTypes = new[] { ClaimTypes.NameIdentifier, "sub", "nameid", "user_id", "id" };

        foreach (var type in candidateTypes)
        {
            var val = User.FindFirstValue(type);
            if (!string.IsNullOrEmpty(val))
            {
                if (ulong.TryParse(val, out var id))
                    return id;

                throw new InvalidOperationException($"无法解析用户ID：claim '{type}' 的值为 '{val}'，不是有效的 ulong。");
            }
        }

        var claimsDump = string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}"));
        throw new InvalidOperationException($"在令牌中未找到用户ID。现有 claims: {claimsDump}");
    }
}
