/**
 * Date utility functions for the uni-app project.
 */

/**
 * Formats a Date object to a local YYYY-MM-DD string.
 */
export function toLocalDateOnlyString(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, "0");
  const d = `${date.getDate()}`.padStart(2, "0");
  return `${y}-${m}-${d}`;
}

/**
 * Parses a YYYY-MM-DD string into a Date object (at midnight local time).
 */
export function parseDateOnly(input: string): Date {
  const parts = input.split("-");
  const y = Number(parts[0] ?? "0") || 0;
  const m = Number(parts[1] ?? "1") || 1;
  const d = Number(parts[2] ?? "1") || 1;
  return new Date(y, m - 1, d);
}
/**
 * 传入一个数，一个长度使用一个字符或者数字填充到指定长度
 * @param num 要填充的数
 * @param length 目标长度
 * @param fillChar 填充字符（默认是 '0'）
 * @returns 填充后的字符串
 */
export function padNumber(
  num: number,
  length: number,
  fillChar: string = "0",
): string {
  return num.toString().padStart(length, fillChar);
}

/**
 * Gets the current date as a YYYY-MM-DD string.
 */
export function getTodayString(): string {
  return toLocalDateOnlyString(new Date());
}

/**
 * Returns an array of Date objects representing each day in a given month.
 */
export function getMonthDays(year: number, month: number): Date[] {
  const days: Date[] = [];
  const cursor = new Date(year, month - 1, 1);
  while (cursor.getMonth() === month - 1) {
    days.push(
      new Date(cursor.getFullYear(), cursor.getMonth(), cursor.getDate()),
    );
    cursor.setDate(cursor.getDate() + 1);
  }
  return days;
}

/**
 * Checks if a date is today.
 */
export function isToday(date: Date | string): boolean {
  const dateStr = typeof date === "string" ? date : toLocalDateOnlyString(date);
  return dateStr === getTodayString();
}

/**
 * Checks if a date is in the past compared to today.
 */
export function isPast(date: Date | string): boolean {
  const dateStr = typeof date === "string" ? date : toLocalDateOnlyString(date);
  return dateStr < getTodayString();
}
