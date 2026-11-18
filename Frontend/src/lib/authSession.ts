import type { AuthUser } from "@/types/auth";

const TOKEN_KEY = "tms_access_token";
const USER_KEY = "tms_user";
const EXPIRY_KEY = "tms_expires_at";
const LEGACY_TOKEN_KEY = "tms_access_token";
const LEGACY_USER_KEY = "tms_user";

export const AUTH_UNAUTHORIZED_EVENT = "tms:unauthorized";

function clearStoredSession(): void {
  if (typeof window === "undefined") return;

  window.sessionStorage.removeItem(TOKEN_KEY);
  window.sessionStorage.removeItem(USER_KEY);
  window.sessionStorage.removeItem(EXPIRY_KEY);
  window.localStorage.removeItem(LEGACY_TOKEN_KEY);
  window.localStorage.removeItem(LEGACY_USER_KEY);
}

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;

  const token = window.sessionStorage.getItem(TOKEN_KEY);
  if (!token) return null;

  const expiresAt = Number(window.sessionStorage.getItem(EXPIRY_KEY));
  if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
    expireStoredSession();
    return null;
  }

  return token;
}

export function saveStoredSession(token: string, user: AuthUser, expiresAt: number): void {
  clearStoredSession();
  window.sessionStorage.setItem(TOKEN_KEY, token);
  window.sessionStorage.setItem(USER_KEY, JSON.stringify(user));
  window.sessionStorage.setItem(EXPIRY_KEY, String(expiresAt));
}

export function clearAuthSession(): void {
  clearStoredSession();
}

export function expireStoredSession(): void {
  clearStoredSession();
  if (typeof window !== "undefined") {
    window.dispatchEvent(new Event(AUTH_UNAUTHORIZED_EVENT));
  }
}

export function getStoredUser(): string | null {
  if (typeof window === "undefined") return null;
  return window.sessionStorage.getItem(USER_KEY);
}

export function getStoredExpiry(): number {
  if (typeof window === "undefined") return 0;
  return Number(window.sessionStorage.getItem(EXPIRY_KEY));
}

export function getTokenExpiry(token: string, responseExpiry: unknown): number {
  const parsedResponseExpiry =
    typeof responseExpiry === "string" ? Date.parse(responseExpiry) : Number.NaN;
  if (Number.isFinite(parsedResponseExpiry) && parsedResponseExpiry > Date.now()) {
    return parsedResponseExpiry;
  }

  const payload = token.split(".")[1];
  if (!payload) return 0;

  try {
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const claims: unknown = JSON.parse(window.atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "=")));
    if (typeof claims === "object" && claims !== null) {
      const expiry = (claims as Record<string, unknown>).exp;
      if (typeof expiry === "number" && Number.isFinite(expiry)) {
        const tokenExpiry = expiry * 1000;
        if (tokenExpiry > Date.now()) return tokenExpiry;
      }
    }
  } catch {
    return 0;
  }

  return 0;
}
