import { defineStore } from "pinia";
import { ref } from "vue";
import { plansApi } from "../api";
import type {
  PlanSummary,
  PlanPayload,
  UpdatePlan,
} from "../types";

export const usePlansStore = defineStore("plans", () => {
  const items = ref<PlanSummary[]>([]);
  const loading = ref<boolean>(false);

  async function fetchMyPlans(): Promise<void> {
    loading.value = true;
    try {
      const response = await plansApi.getPlans();
      items.value = response.data;
    } finally {
      loading.value = false;
    }
  }

  async function createPlan(payload: PlanPayload): Promise<PlanSummary> {
    const response = await plansApi.CreatePlan(payload);
    const created = response.data;
    items.value.push(created);
    return created;
  }

  async function updatePlan(payload: UpdatePlan): Promise<void> {
    await plansApi.UpdatePlan(payload);
    // Ideally we should just refetch to get updated server state (especially IDs for new time slots)
    await fetchMyPlans();
  }

  async function deletePlan(id: number): Promise<void> {
    await plansApi.deletePlan(id);
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
    deletePlan,
  };
});
