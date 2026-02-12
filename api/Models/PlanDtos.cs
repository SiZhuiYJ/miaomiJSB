using System;

namespace api.Models;

/// <summary>
/// 创建打卡计划的请求参数。
/// </summary>
public class CreatePlanRequest
{
    /// <summary>
    /// 打卡计划标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 计划描述，可为空。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 计划开始日期，不传则默认为当天。
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// 计划结束日期，可为空。
    /// </summary>
    public DateOnly? EndDate { get; set; }

    public List<PlanTimeSlot>? DailyTimeSlots { get; set; }
}
/// <summary>
/// 更新打卡计划的请求参数。
/// </summary>
public class UpdatePlanRequest
{
    /// <summary>
    /// 打卡计划唯一标识。
    /// </summary>
    public ulong Id { get; set; }
    /// <summary>
    /// 打卡计划标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 计划描述，可为空。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否处于启用状态。
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 计划开始日期，不传则默认为当天。
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// 计划结束日期，可为空。
    /// </summary>
    public DateOnly? EndDate { get; set; }

    public List<PlanTimeSlot>? DailyTimeSlots { get; set; }
}
/// <summary>
/// 打卡计划概要信息，用于列表展示。
/// </summary>
public class PlanSummary
{
    /// <summary>
    /// 计划唯一标识。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 计划标题。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 计划描述，可为空。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 计划开始日期。
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// 计划结束日期，可为空。
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// 是否处于启用状态。
    /// </summary>
    public bool? IsActive { get; set; }

    public List<PlanTimeSlot>? DailyTimeSlots { get; set; }
}

public class PlanTimeSlot
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
}
