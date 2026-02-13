using System;
using System.Collections.Generic;

namespace api.Models;

/// <summary>
/// 当日打卡请求参数。
/// </summary>
public class DailyCheckinRequest
{
    /// <summary>
    /// 对应的打卡计划 ID。
    /// </summary>
    public long PlanId { get; set; }

    /// <summary>
    /// 打卡图片 URL 列表，可为空。
    /// </summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// 打卡备注信息，可为空。
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 打卡时间段 ID，如果计划开启了分时段打卡则必填。
    /// </summary>
    public ulong? TimeSlotId { get; set; }
}

/// <summary>
/// 补打卡请求参数。
/// </summary>
public class RetroCheckinRequest
{
    /// <summary>
    /// 对应的打卡计划 ID。
    /// </summary>
    public long PlanId { get; set; }

    /// <summary>
    /// 需要补打卡的日期。
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 打卡图片 URL 列表，可为空。
    /// </summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// 补打卡备注信息，可为空。
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// 日历打卡状态项。
/// </summary>
public class CalendarStatusItem
{
    /// <summary>
    /// 日期。
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 打卡状态：1=正常打卡，2=补打卡等。
    /// </summary>
    public short Status { get; set; }
}

/// <summary>
/// 某一天的打卡详情返回结果。
/// </summary>
public class CheckinDetailResponse
{
    /// <summary>
    /// 打卡日期。
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 打卡状态：0错过、1正常打卡、2补打卡。
    /// </summary>
    public sbyte Status { get; set; }

    /// <summary>
    /// 打卡备注信息，可为空。
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 打卡图片 URL 列表。
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>
    /// 打卡时间段 ID。
    /// </summary>
    public ulong? TimeSlotId { get; set; }

    /// <summary>
    /// 打卡时间段名称。
    /// </summary>
    public string? SlotName { get; set; }
}
