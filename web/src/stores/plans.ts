import { defineStore } from 'pinia';
import { http } from '../api/http';
import { ref } from 'vue';

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
  description: string | null;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
  timeSlots?: TimeSlotDto[];
}

export const usePlansStore = defineStore('plans', () => {
  const items = ref<PlanSummary[]>([]);
  const loading = ref<boolean>(false);

  async function fetchMyPlans(): Promise<void> {
    loading.value = true;
    try {
      const response = await http.get<PlanSummary[]>('/mm/Plans');
      items.value = response.data;
    } finally {
      loading.value = false;
    }
  }

  async function createPlan(payload: {
    title: string;
    description?: string;
    startDate?: string | null;
    endDate?: string | null;
    timeSlots?: TimeSlotDto[];
  }): Promise<PlanSummary> {
    const response = await http.post<PlanSummary>('/mm/Plans', payload);
    const created = response.data;
    items.value.push(created);
    return created;
  }

  async function updatePlan(payload: {
    id: number;
    title?: string;
    description?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    isActive: boolean;
    timeSlots?: TimeSlotDto[];
  }): Promise<void> {
    await http.post('/mm/Plans/update', payload);
    // Ideally we should just refetch to get updated server state (especially IDs for new time slots)
    await fetchMyPlans();
  }

  async function deletePlan(id: number): Promise<void> {
    await http.post(`/mm/Plans/delete?PlanId=${id}`);
    const index = items.value.findIndex((x) => x.id === id);
    if (index !== -1) {
      items.value.splice(index, 1);
    }
  }

  return {
    items,
    loading,
    fetchMyPlans,
    createPlan,
    updatePlan,
    deletePlan
  };
});