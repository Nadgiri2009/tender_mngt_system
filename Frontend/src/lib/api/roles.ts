import { apiClient, toApiError } from "./client";
import type { Role } from "@/types/auth";

export interface RegistrationAccount {
  accountType: "ExternalBidder";
  roleName: "External bidder";
}

export interface PermissionCatalogItem {
  permissionKey: string;
  description: string;
}

export interface RolePermissionItem extends Role {
  permissionKeys: string[];
}

export interface RolePermissionMatrix {
  roles: RolePermissionItem[];
  permissions: PermissionCatalogItem[];
}

// GET /api/roles
export async function fetchRoles(): Promise<Role[]> {
  try {
    const { data } = await apiClient.get<Role[]>("/roles");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchRegistrationRole(): Promise<RegistrationAccount> {
  try {
    const { data } = await apiClient.get<RegistrationAccount>("/roles/registration");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchRolePermissionMatrix(): Promise<RolePermissionMatrix> {
  try {
    const { data } = await apiClient.get<RolePermissionMatrix>("/roles/permissions");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function updateRolePermissions(roleId: number, permissionKeys: string[]): Promise<void> {
  try {
    await apiClient.put(`/roles/${roleId}/permissions`, { permissionKeys });
  } catch (error) {
    throw toApiError(error);
  }
}
