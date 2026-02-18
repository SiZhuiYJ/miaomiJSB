import { defineStore } from 'pinia';
import { http } from '../api/http';
import { ref } from 'vue';

export interface CalendarItem {
  date: string;
  status: number;
}

export interface CheckinDetail {
  date: string;
  status: number;
  note?: string;
  imageUrls: string[];
  timeSlotId?: number;
}

export const useCheckinsStore = defineStore('checkins', () => {
  const calendar = ref<CalendarItem[]>([]);
  const calendarByPlan = ref<Record<number, CalendarItem[]>>({});

  async function loadCalendar(planId: number, year: number, month: number): Promise<void> {
    const response = await http.get<CalendarItem[]>(
      '/mm/Checkins/calendar',
      { params: { planId, year, month } },
    );
    calendar.value = response.data;
    calendarByPlan.value[planId] = response.data;
  }

  async function dailyCheckin(payload: {
    planId: number;
    imageUrls?: string[];
    note?: string;
    timeSlotId?: number;
  }): Promise<void> {
    await http.post('/mm/Checkins/daily', payload);
  }

  async function retroCheckin(payload: {
    planId: number;
    date: string;
    imageUrls?: string[];
    note?: string;
    timeSlotId?: number;
  }): Promise<void> {
    await http.post('/mm/Checkins/retro', payload);
  }

  async function getCheckinDetail(planId: number, date: string): Promise<CheckinDetail[]> {
    const response = await http.get<CheckinDetail[]>(
      '/mm/Checkins/detail',
      { params: { planId, date } },
    );
    return response.data;
  }

  return {
    calendar,
    calendarByPlan,
    loadCalendar,
    dailyCheckin,
    retroCheckin,
    getCheckinDetail
  };
});