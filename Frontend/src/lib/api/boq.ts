import { apiClient, toApiError } from "./client";
import type { BOQItem } from "@/types/tender";
import type { BOQItemFormValues } from "@/lib/validations/tender";

// GET /api/boq/tender/{tenderId} -- BOQController.GetByTender
export async function fetchBOQItems(tenderId: number): Promise<BOQItem[]> {
  try {
    const { data } = await apiClient.get<BOQItem[]>(`/boq/tender/${tenderId}`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/boq -- BOQController.Create, body is CreateBOQItemDto (includes tenderId)
export async function addBOQItem(
  tenderId: number,
  input: BOQItemFormValues
): Promise<BOQItem> {
  try {
    const { data } = await apiClient.post<BOQItem>("/boq", { tenderId, ...input });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// PUT /api/boq/{id} -- BOQController.Update, body is UpdateBOQItemDto (includes tenderId)
export async function updateBOQItem(
  boqId: number,
  tenderId: number,
  input: BOQItemFormValues
): Promise<BOQItem> {
  try {
    const { data } = await apiClient.put<BOQItem>(`/boq/${boqId}`, { tenderId, ...input });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// DELETE /api/boq/{id} -- BOQController.Delete
export async function deleteBOQItem(boqId: number): Promise<void> {
  try {
    await apiClient.delete(`/boq/${boqId}`);
  } catch (error) {
    throw toApiError(error);
  }
}
