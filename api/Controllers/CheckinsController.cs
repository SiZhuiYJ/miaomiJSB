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
/// 打卡记录管理接口，提供完整的打卡功能实现。
/// 支持日常打卡、补打卡、时间段打卡等多种打卡模式。
/// 提供打卡日历查询、详细记录查询等统计分析功能。
/// 实现严格的时效性控制和数据完整性验证。
/// </summary>
/// <remarks>
/// 打卡控制器，负责处理用户的所有打卡相关操作。
/// 包含时间验证、重复打卡检测、图片验证等核心业务逻辑。
/// </remarks>
/// <param name="db">打卡系统数据库上下文，用于数据持久化操作。</param>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class CheckinsController(DailyCheckDbContext db) : ControllerBase
{

    /// <summary>
    /// 用户当日打卡接口，用于记录用户当天的打卡行为。
    /// 支持时间段打卡模式，验证打卡时间是否在有效范围内。
    /// 强制要求上传1-3张图片作为打卡凭证。
    /// 自动检测重复打卡，防止同一时间段多次打卡。
    /// </summary>
    /// <param name="request">当日打卡请求参数，包含计划ID、图片URL和备注。</param>
    /// <returns>
    /// 成功时返回200 OK；
    /// 失败时返回相应错误状态码和详细信息。
    /// </returns>
    /// <response code="200">打卡成功</response>
    /// <response code="400">参数错误、计划未开始、时间不在范围内或图片数量不符合要求</response>
    /// <response code="404">计划不存在</response>
    /// <response code="409">当天已打卡（重复打卡）</response>
    [HttpPost("daily")]
    public async Task<ActionResult> Daily(DailyCheckinRequest request)
    {
        var userId = GetUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var plan = await db.CheckinPlans
            .Include(p => p.CheckinPlanTimeSlots)
            .SingleOrDefaultAsync(x => x.Id == (ulong)request.PlanId && x.UserId == userId && !x.IsDeleted && x.IsActive == true);
        if (plan == null)
            return NotFound("未找到计划");

        if (plan.StartDate > today)
            return BadRequest("计划尚未开始");

        // 分时段打卡逻辑
        CheckinPlanTimeSlot? timeSlot = null;
        if (request.TimeSlotId.HasValue)
        {
            timeSlot = plan.CheckinPlanTimeSlots.FirstOrDefault(ts => ts.Id == request.TimeSlotId.Value && ts.IsActive == true);
            if (timeSlot == null)
                return BadRequest("无效的时间段");

            // 验证时间是否在范围内
            // 假设服务器时区与用户一致，或用户接受服务器时间。这里使用 DateTime.UtcNow + 8 (CST)
            var now = DateTime.UtcNow.AddHours(8);
            var currentTime = TimeOnly.FromDateTime(now);

            if (currentTime < timeSlot.StartTime)
                return BadRequest($"时间段 {timeSlot.SlotName} 尚未开始 ({timeSlot.StartTime})");
            if (currentTime > timeSlot.EndTime)
                return BadRequest($"时间段 {timeSlot.SlotName} 已结束 ({timeSlot.EndTime})，请使用补打卡");
        }
        else if (plan.CheckinPlanTimeSlots.Any(ts => ts.IsActive == true))
        {
            return BadRequest("此计划需要选择时间段");
        }

        var existing = await db.Checkins
            .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.CheckDate == today && x.TimeSlotId == request.TimeSlotId && !x.IsDeleted);
        if (existing != null)
            return Conflict("今天已在此时间段打卡" + (request.TimeSlotId.HasValue ? "" : ""));

        if (request.ImageUrls == null || request.ImageUrls.Count < 1 || request.ImageUrls.Count > 3)
            return BadRequest("日常打卡必须包含1-3张图片");

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
    /// 补打卡接口，用于补录历史日期的打卡记录。
    /// 严格限制补打卡时间范围，防止滥用。
    /// 支持时间段补打卡，验证补打卡时机合理性。
    /// 区分正常打卡和补打卡状态，便于统计分析。
    /// </summary>
    /// <param name="request">补打卡请求参数，包含计划ID、补打日期、图片URL和备注。</param>
    /// <returns>
    /// 成功时返回200 OK；
    /// 失败时返回相应错误状态码和详细信息。
    /// </returns>
    /// <response code="200">补打卡成功</response>
    /// <response code="400">日期无效、计划未激活、时间验证失败或图片数量不符合要求</response>
    /// <response code="404">计划不存在</response>
    /// <response code="409">指定日期已存在打卡记录</response>
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
            return NotFound("未找到计划");

        if (plan.StartDate > request.Date)
            return BadRequest("该日期计划尚未激活");

        // 分时段逻辑
        if (request.TimeSlotId.HasValue)
        {
            var slot = plan.CheckinPlanTimeSlots.FirstOrDefault(ts => ts.Id == request.TimeSlotId.Value && ts.IsActive == true);
            if (slot == null)
                return BadRequest("无效的时间段");

            if (request.Date == today)
            {
                var slotEnd = request.Date.ToDateTime(slot.EndTime);
                if (now <= slotEnd)
                    return BadRequest($"当前时间在时间段 {slot.SlotName} 内，请使用日常打卡");
            }
        }
        else if (plan.CheckinPlanTimeSlots.Any(ts => ts.IsActive == true))
        {
            return BadRequest("此计划需要选择时间段");
        }
        else if (request.Date == today)
        {
            var dayEnd = request.Date.ToDateTime(new TimeOnly(23, 59, 59));
            if (now <= dayEnd)
                return BadRequest("当前时间在打卡日内，请使用日常打卡");
        }

        var existing = await db.Checkins
            .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.CheckDate == request.Date && x.TimeSlotId == request.TimeSlotId && !x.IsDeleted);
        if (existing != null)
            return Conflict("该日期已在此时间段打卡" + (request.TimeSlotId.HasValue ? "" : ""));

        if (request.ImageUrls == null || request.ImageUrls.Count < 1 || request.ImageUrls.Count > 3)
            return BadRequest("补打卡必须包含1-3张图片");

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
    /// 获取指定打卡计划的月度打卡日历状态接口。
    /// 支持两种打卡模式：默认模式和时间段模式。
    /// 默认模式下统计每日整体完成情况；时间段模式下显示每段时间的详细状态。
    /// 返回数据可用于前端日历组件的状态展示。
    /// </summary>
    /// <param name="planId">打卡计划ID，必须是当前用户拥有的有效计划。</param>
    /// <param name="year">查询年份，如2024。</param>
    /// <param name="month">查询月份，1-12之间的整数。</param>
    /// <returns>
    /// 成功时返回200 OK，包含该月所有有打卡记录的日期状态；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">获取日历状态成功</response>
    /// <response code="404">计划不存在或无权限访问</response>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<CalendarStatusItem>>> GetCalendar(long planId, int year, int month)
    {
        var userId = GetUserId();
        var plan = await db.CheckinPlans.SingleOrDefaultAsync(x => x.Id == (ulong)planId && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
            return NotFound("未找到计划");

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
                    int overallStatus = completedSlots; // 在时间段打卡模式下，展示已打卡的时间段数量
                    result.Add(new CalendarStatusItem
                    {
                        Date = currentDate,
                        Status = (sbyte)overallStatus,
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
    /// 获取指定日期的打卡详情接口。
    /// 返回特定日期的所有打卡记录，包括图片、备注等详细信息。
    /// 支持单日多次打卡（不同时间段）的详情查询。
    /// 图片URL可直接用于前端展示。
    /// </summary>
    /// <param name="planId">打卡计划ID，必须是当前用户拥有的有效计划。</param>
    /// <param name="date">需要查询的具体日期，格式为yyyy-MM-dd。</param>
    /// <returns>
    /// 成功时返回200 OK，包含该日期的所有打卡详情列表；
    /// 如果无打卡记录则返回404 Not Found。
    /// </returns>
    /// <response code="200">获取打卡详情成功</response>
    /// <response code="404">计划不存在或指定日期无打卡记录</response>
    [HttpGet("detail")]
    public async Task<ActionResult<List<CheckinDetailResponse>>> GetDetail(long planId, DateOnly date)
    {
        var userId = GetUserId();

        var plan = await db.CheckinPlans.SingleOrDefaultAsync(x => x.Id == (ulong)planId && x.UserId == userId && !x.IsDeleted);
        if (plan == null)
            return NotFound("未找到计划");

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
