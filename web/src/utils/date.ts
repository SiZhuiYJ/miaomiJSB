/**
 * 将颜色加深指定比例
 * @param date 日期 格式New Date()
 * @returns 年月日，格式 'yyyy-mm-dd'
 */
export function toLocalDateOnlyString(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, "0");
  const d = `${date.getDate()}`.padStart(2, "0");
  return `${y}-${m}-${d}`;
}
