import { apiClient, toApiError } from "./client";
import type {
  AuthResponse,
  LoginInput,
  RegisterInput,
  RegisterResponse,
  RequestLoginOtpInput,
  RequestLoginOtpResponse,
} from "@/types/auth";

// POST /api/auth/request-otp
export async function requestLoginOtp(input: RequestLoginOtpInput): Promise<RequestLoginOtpResponse> {
  try {
    const { data } = await apiClient.post<RequestLoginOtpResponse>("/auth/request-otp", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/auth/login verifies a one-time mobile code.
export async function login(input: LoginInput): Promise<AuthResponse> {
  try {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

// POST /api/auth/register
export async function register(input: RegisterInput): Promise<RegisterResponse> {
  try {
    const { data } = await apiClient.post<RegisterResponse>("/auth/register", input);
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}
