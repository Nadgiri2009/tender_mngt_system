"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";

export function useRequireRole(allowedRoles: string[]) {
  const { user, isLoading, isAuthenticated } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (user && !allowedRoles.includes(user.role)) {
      router.replace("/tenders");
    }
  }, [isLoading, isAuthenticated, user, router, allowedRoles]);

  const isAuthorized = !isLoading && isAuthenticated && !!user && allowedRoles.includes(user.role);

  return { isLoading, isAuthorized };
}
