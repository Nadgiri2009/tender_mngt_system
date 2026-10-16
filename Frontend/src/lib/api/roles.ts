import { apiClient, toApiError } from "./client";
import type { Role } from "@/types/auth";

// GET /api/roles
export async function fetchRoles(): Promise<Role[]> {
  try {
    const { data } = await apiClient.get<Role[]>("/roles");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchRegistrationRole(): Promise<Role> {
  try {
    const { data } = await apiClient.get<Role>("/roles/registration");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
