import { defineStore } from 'pinia';
import { http } from '../utils/http';

export interface TimeSlotDto {
  id?: number;
  slotName?: string;
  startTime: string; // HH:mm:ss
  endTime: string; // HH:mm:ss
  orderNum?: number;
  isActive?: boolean;
}

export interface PlanSummary {
  id: number;
  title: string;
  description: string;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
  timeSlots?: TimeSlotDto[];
}

interface PlansState {
  items: PlanSummary[];
  loading: boolean;
}

export const usePlansStore = defineStore('plans', {
  state: (): PlansState => ({
    items: [],
    loading: false,
  }),
  actions: {
    async fetchMyPlans(): Promise<void> {
      this.loading = true;
      try {
        const response = await http.get<PlanSummary[]>('/mm/Plans');
        this.items = response.data;
      } finally {
        this.loading = false;
      }
    },
    async createPlan(payload: {
      title: string;
      description?: string;
      startDate?: string | null;
      endDate?: string | null;
      timeSlots?: TimeSlotDto[];
    }): Promise<PlanSummary> {
      const response = await http.post<PlanSummary>('/mm/Plans', payload);
      const created = response.data;
      this.items.push(created);
      return created;
    },
    async updatePlan(payload: {
      id: number;
      title?: string;
      description?: string;
      startDate?: string | null;
      endDate?: string | null;
      isActive: boolean;
      timeSlots?: TimeSlotDto[];
    }): Promise<void> {
      await http.post('/mm/Plans/update', payload);
      const index = this.items.findIndex((x) => x.id === payload.id);
      if (index !== -1) {
        // Handle startDate possibly being undefined/null in payload, but required in state
        const updated = { ...this.items[index], ...payload };
        if (updated.startDate === null || updated.startDate === undefined) {
          updated.startDate = this.items[index].startDate;
        }
        if (payload.timeSlots) {
          updated.timeSlots = payload.timeSlots;
        }
        this.items[index] = updated as PlanSummary;
      }
    },
    async deletePlan(id: number): Promise<void> {
      await http.post(`/mm/Plans/delete?PlanId=${id}`);
      const index = this.items.findIndex((x) => x.id === id);
      if (index !== -1) {
        this.items.splice(index, 1);
      }
    },
  },
});
