import axios, { AxiosError } from "axios";

// NOTE: Login/auth is out of scope for this module -- it's a separate module
// in the master spec. This client assumes a JWT is already present under
// this key, written by whatever the auth module ends up being. Until that
// exists, nothing behind [Authorize] on the backend will work from this UI.
const TOKEN_STORAGE_KEY = "tms_access_token";

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_STORAGE_KEY);
}

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Matches the JSON shape written by Backend/.../Middleware/GlobalExceptionMiddleware.cs:
// { success: false, statusCode: number, message: string }
interface BackendErrorPayload {
  success: boolean;
  statusCode: number;
  message: string;
}

export class ApiError extends Error {
  statusCode: number;

  constructor(message: string, statusCode: number) {
    super(message);
    this.name = "ApiError";
    this.statusCode = statusCode;
  }
}

export function toApiError(error: unknown): ApiError {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<BackendErrorPayload>;
    const payload = axiosError.response?.data;
    const requestUrl = `${axiosError.config?.baseURL ?? ""}${axiosError.config?.url ?? ""}`;

    // A 404 during auth calls usually means the frontend is pointed at the wrong API base URL/port.
    if (axiosError.response?.status === 404 && axiosError.config?.url?.includes("/auth/")) {
      return new ApiError(
        `Auth endpoint not found at ${requestUrl}. Check NEXT_PUBLIC_API_BASE_URL (expected API host/port, e.g. http://localhost:5000/api).`,
        404
      );
    }

    if (payload?.message) {
      return new ApiError(payload.message, axiosError.response?.status ?? 0);
    }
    if (axiosError.code === "ERR_NETWORK") {
      return new ApiError(
        `Could not reach the server at ${axiosError.config?.baseURL ?? "configured API URL"}. Check that the API is running and NEXT_PUBLIC_API_BASE_URL is correct.`,
        0
      );
    }
    return new ApiError(axiosError.message, axiosError.response?.status ?? 0);
  }
  if (error instanceof Error) {
    return new ApiError(error.message, 0);
  }
  return new ApiError("An unknown error occurred.", 0);
}
