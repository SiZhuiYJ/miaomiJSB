import { defineStore } from 'pinia';
import { http } from '../api/http';

export interface PlanSummary {
  id: number;
  title: string;
  description: string | null;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
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
    }): Promise<PlanSummary> {
      const response = await http.post<PlanSummary>('/mm/Plans', payload);
      const created = response.data;
      this.items.push(created);
      return created;
    },
    async updatePlan(payload: {
      id: number;
      title?: string;
      description?: string | null;
      startDate?: string | null;
      endDate?: string | null;
      isActive: boolean;
    }): Promise<void> {
      await http.post('/mm/Plans/update', payload);
      const index = this.items.findIndex((x) => x.id === payload.id);
      if (index === -1) return;
      const current = this.items[index]!;
      if (payload.title !== undefined) current.title = payload.title;
      if (payload.description !== undefined) current.description = payload.description;
      if (payload.startDate !== undefined && payload.startDate !== null) current.startDate = payload.startDate;
      if (payload.endDate !== undefined) current.endDate = payload.endDate;
      current.isActive = payload.isActive;
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
