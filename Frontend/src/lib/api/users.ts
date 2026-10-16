import { apiClient, toApiError } from "./client";

export interface AdminUser {
  userId: number;
  fullName: string;
  email: string;
  mobile: string | null;
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
