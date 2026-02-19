// @/libs/api/auth/index.ts
import http from "@/libs/http";
import type { PlanSummary, PlanPayload, UpdatePlan } from "../types";

export const plansApi = {
  async getPlans() {
    return await http.get<PlanSummary[]>("/mm/Plans");
  },
  async CreatePlan(data: PlanPayload) {
    return await http.post<PlanSummary>("/mm/Plans", data);
  },
  async UpdatePlan(data: UpdatePlan) {
    return await http.post("/mm/Plans/update", data);
  },
  async deletePlan(id: number) {
    return await http.post(`/mm/Plans/delete?PlanId=${id}`);
  },
};
