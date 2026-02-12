import { defineStore } from 'pinia';
import { http } from '../api/http';

export interface CalendarItem {
  date: string;
  status: number;
}

export interface CheckinDetail {
  date: string;
  status: number;
  note?: string;
  imageUrls: string[];
}

interface CheckinsState {
  calendar: CalendarItem[];
  calendarByPlan: Record<number, CalendarItem[]>;
}

export const useCheckinsStore = defineStore('checkins', {
  state: (): CheckinsState => ({
    calendar: [],
    calendarByPlan: {},
  }),
  actions: {
    async loadCalendar(planId: number, year: number, month: number): Promise<void> {
      const response = await http.get<CalendarItem[]>(
        '/mm/Checkins/calendar',
        { params: { planId, year, month } },
      );
      this.calendar = response.data;
      this.calendarByPlan[planId] = response.data;
    },
    async dailyCheckin(payload: {
      planId: number;
      imageUrls?: string[];
      note?: string;
    }): Promise<void> {
      await http.post('/mm/Checkins/daily', payload);
    },
    async retroCheckin(payload: {
      planId: number;
      date: string;
      imageUrls?: string[];
      note?: string;
    }): Promise<void> {
      await http.post('/mm/Checkins/retro', payload);
    },
    async getCheckinDetail(planId: number, date: string): Promise<CheckinDetail> {
      const response = await http.get<CheckinDetail>(
        '/mm/Checkins/detail',
        { params: { planId, date } },
      );
      return response.data;
    },
  },
});
