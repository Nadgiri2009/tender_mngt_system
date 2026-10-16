import { apiClient, toApiError } from "./client";

export async function fetchTenderStatuses(): Promise<string[]> {
  try {
    const { data } = await apiClient.get<string[]>("/lookups/tender-statuses");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchTenderTypes(): Promise<string[]> {
  try {
    const { data } = await apiClient.get<string[]>("/lookups/tender-types");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
