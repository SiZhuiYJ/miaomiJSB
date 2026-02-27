/**
 * 将日期转换为年月日字符串
 * @param date 日期 格式New Date()
 * @returns 年月日，格式 'yyyy-mm-dd'
 */
export function toLocalDateOnlyString(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, "0");
  const d = `${date.getDate()}`.padStart(2, "0");
  return `${y}-${m}-${d}`;
}

/**
 * 判断date是否为今天
 * @param date 日期 格式New Date()
 * @returns 判断(true or false) boolean
 */
export function isToday(date: Date): boolean {
  const today = new Date();
  return (
    date.getFullYear() === today.getFullYear() &&
    date.getMonth() === today.getMonth() &&
    date.getDate() === today.getDate()
  );
}
