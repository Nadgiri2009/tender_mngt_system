"use client";

import { createContext, useContext, useEffect, useState, useCallback } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { AuthResponse, AuthUser } from "@/types/auth";
import {
  AUTH_SESSION_ERROR_KEY,
  AUTH_UNAUTHORIZED_EVENT,
  clearAuthSession,
  getStoredExpiry,
  getStoredToken,
  getStoredUser,
  getTokenExpiry,
  saveStoredSession,
} from "@/lib/authSession";

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  setSession: (auth: AuthResponse) => boolean;
  updateProfile: (profile: Pick<AuthUser, "fullName" | "email">) => boolean;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();
  const pathname = usePathname() ?? "";
  const queryClient = useQueryClient();

  useEffect(() => {
    const stored = getStoredUser();
    const token = getStoredToken();
    const expiresAt = getStoredExpiry();

    if (stored && token && expiresAt > Date.now()) {
      try {
        const parsed: unknown = JSON.parse(stored);
        if (isAuthUser(parsed)) {
          setUser(parsed);
        } else {
          clearAuthSession();
        }
      } catch {
        clearAuthSession();
      }
    } else {
      clearAuthSession();
    }
    setIsLoading(false);
  }, []);

  const setSession = useCallback((auth: AuthResponse) => {
    const expiresAt = getTokenExpiry(auth.token, auth.expiresAt);
    if (!auth.token || !expiresAt) {
      clearAuthSession();
      setUser(null);
      return false;
    }

    const authUser: AuthUser = {
      userId: auth.userId,
      fullName: auth.fullName,
      email: auth.email,
      role: auth.role,
      accountType: auth.accountType,
      departmentId: auth.departmentId,
    };
    saveStoredSession(auth.token, authUser, expiresAt);
    setUser(authUser);
    return true;
  }, []);

  const updateProfile = useCallback((profile: Pick<AuthUser, "fullName" | "email">) => {
    if (!user) return false;
    const token = getStoredToken();
    const expiresAt = getStoredExpiry();
    if (!token || expiresAt <= Date.now()) return false;

    const updatedUser = { ...user, ...profile };
    saveStoredSession(token, updatedUser, expiresAt);
    setUser(updatedUser);
    return true;
  }, [user]);

  const logout = useCallback(() => {
    clearAuthSession();
    queryClient.clear();
    setUser(null);
    router.replace("/");
  }, [queryClient, router]);

  useEffect(() => {
    const showPendingSessionError = () => {
      const message = window.sessionStorage.getItem(AUTH_SESSION_ERROR_KEY);
      if (message) {
        window.sessionStorage.removeItem(AUTH_SESSION_ERROR_KEY);
        toast.error(message);
      }
    };

    if (pathname.startsWith("/login")) {
      showPendingSessionError();
    }

    const handleUnauthorized = () => {
      queryClient.clear();
      setUser(null);

      if (!pathname.startsWith("/login") && !pathname.startsWith("/register")) {
        router.replace("/login");
      } else {
        showPendingSessionError();
      }
    };

    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
  }, [pathname, queryClient, router, user]);

  return (
    <AuthContext.Provider value={{ user, isLoading, isAuthenticated: !!user, setSession, updateProfile, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

function isAuthUser(value: unknown): value is AuthUser {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.userId === "number" &&
    typeof candidate.fullName === "string" &&
    typeof candidate.email === "string" &&
    typeof candidate.role === "string" &&
    typeof candidate.accountType === "string" &&
    (candidate.departmentId === null || typeof candidate.departmentId === "number")
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
