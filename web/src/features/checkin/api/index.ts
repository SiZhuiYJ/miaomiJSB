// @/libs/api/checkin/index.ts
import http from "@/libs/http";
import type {
  CalendarItem,
  CheckinDetail,
  CheckinRecord,
  retroCheckinRecord,
} from "../types";

export const checkinApi = {
  async loadCalendar(data: {
    params: { planId: number; year: number; month: number };
  }) {
    return await http.get<CalendarItem[]>("/mm/Checkins/calendar", data);
  },
  async Checkin(data: CheckinRecord) {
    return await http.post("/mm/Checkins/daily", data);
  },
  async RetroCheckin(data: retroCheckinRecord) {
    return http.post("/mm/Checkins/retro", data);
  },
  async GetCheckinDetail(data: { params: { planId: number; date: string } }) {
    return await http.get<CheckinDetail[]>("/mm/Checkins/detail", data);
  },
};
