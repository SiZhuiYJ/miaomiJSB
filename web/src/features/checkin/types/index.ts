export interface CalendarItem {
  date: string;
  status: number;
}

export interface CheckinDetail {
  id: number;
  date: string;
  status: number;
  note?: string;
  imageUrls: string[];
  timeSlotId?: number;
}
export interface CheckinRecord {
  planId: number;
  imageUrls?: string[];
  note?: string;
  timeSlotId?: number;
}
export interface retroCheckinRecord extends CheckinRecord {
  date: string;
}
