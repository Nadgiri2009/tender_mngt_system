import { apiClient, toApiError } from "./client";
import { getStoredToken } from "@/lib/authSession";
import type { Department, PagedResult } from "@/types/tender";

export interface DepartmentInput {
  departmentName: string;
  departmentCode?: string;
  headOfficerName?: string;
  isActive?: boolean;
}

export interface AdminDepartmentQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
}

export async function fetchAdminDepartments(query: AdminDepartmentQuery): Promise<PagedResult<Department>> {
  try {
    const { data } = await apiClient.get<PagedResult<Department>>("/departments/paged", {
      params: {
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
        search: query.search || undefined,
      },
    });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchDepartments(): Promise<Department[]> {
  try {
    const endpoint = getStoredToken() ? "/departments" : "/dashboard/public-departments";
    const { data } = await apiClient.get<Department[]>(endpoint);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchPublicDepartments(): Promise<Department[]> {
  try {
    const { data } = await apiClient.get<Department[]>("/dashboard/public-departments");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function createDepartment(input: DepartmentInput): Promise<Department> {
  try {
    const { data } = await apiClient.post<Department>("/departments", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function updateDepartment(id: number, input: DepartmentInput): Promise<Department> {
  try {
    const { data } = await apiClient.put<Department>(`/departments/${id}`, input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function deleteDepartment(id: number): Promise<{ message?: string }> {
  try {
    const { data } = await apiClient.delete<{ message?: string }>(`/departments/${id}`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
