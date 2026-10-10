import { apiClient, toApiError } from "./client";
import type { AuthResponse, LoginInput, RegisterInput } from "@/types/auth";

// POST /api/auth/login
export async function login(input: LoginInput): Promise<AuthResponse> {
  try {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/auth/register
export async function register(input: RegisterInput): Promise<AuthResponse> {
  try {
    const { data } = await apiClient.post<AuthResponse>("/auth/register", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
