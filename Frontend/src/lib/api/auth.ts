import { apiClient, toApiError } from "./client";
import type {
  AuthResponse,
  LoginInput,
  RegisterInput,
  RegisterResponse,
  RequestLoginOtpInput,
  RequestLoginOtpResponse,
  UpdateUserProfileInput,
  UserProfile,
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
    const { data } = await apiClient.post<AuthResponse>("/auth/login", input, { timeout: 15_000 });
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function fetchMyProfile(): Promise<UserProfile> {
  try {
    const { data } = await apiClient.get<UserProfile>("/auth/profile");
    return data;
  } catch (error) {
    throw toApiError(error);
  }
}

export async function updateMyProfile(input: UpdateUserProfileInput): Promise<UserProfile> {
  try {
    const { data } = await apiClient.put<UserProfile>("/auth/profile", input);
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
