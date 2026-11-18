import { apiClient, toApiError } from "./client";
import type { PagedResult } from "@/types/tender";

export interface AdminUser {
  userId: number;
  fullName: string;
  email: string;
  mobile: string | null;
  accountType: string;
  roleName: string;
  departmentName: string | null;
  isActive: boolean;
  createdDate: string;
}

export interface CreateStaffUserInput {
  fullName: string;
  email: string;
  mobile: string;
  password: string;
  roleId: number;
  departmentId?: number;
}

export interface UpdateStaffUserInput {
  fullName: string;
  email: string;
  mobile: string;
  roleId: number;
  departmentId?: number;
}

export interface AdminUserQuery {
  pageNumber: number;
  pageSize: number;
  search?: string;
}

export async function fetchUsersPage(query: AdminUserQuery): Promise<PagedResult<AdminUser>> {
  try {
    const { data } = await apiClient.get<PagedResult<AdminUser>>("/users/paged", {
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

// GET /api/users - Admin only (Controllers/AdminController.cs -> UsersController)
export async function fetchUsers(): Promise<AdminUser[]> {
  try {
    const { data } = await apiClient.get<AdminUser[]>("/users");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/users - Admin only, creates staff account with any role
export async function createStaffUser(input: CreateStaffUserInput): Promise<AdminUser> {
  try {
    const { data } = await apiClient.post<AdminUser>("/users", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// PUT /api/users/{id}/toggle-active - Admin only
export async function toggleUserActive(id: number): Promise<{ userId: number; isActive: boolean }> {
  try {
    const { data } = await apiClient.put(`/users/${id}/toggle-active`);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function updateStaffUser(id: number, input: UpdateStaffUserInput): Promise<AdminUser> {
  try {
    const { data } = await apiClient.put<AdminUser>(`/users/${id}`, input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function resetUserPassword(id: number, password: string): Promise<void> {
  try {
    await apiClient.put(`/users/${id}/reset-password`, { password });
  } catch (error) {
    throw toApiError(error);
  }
}

export async function deleteUser(id: number): Promise<void> {
  try {
    await apiClient.delete(`/users/${id}`);
  } catch (error) {
    throw toApiError(error);
  }
}
