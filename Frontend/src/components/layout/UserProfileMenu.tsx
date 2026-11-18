"use client";

import { ChevronDown, CircleUserRound, LogOut } from "lucide-react";

import { useAuth } from "@/contexts/AuthContext";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface UserProfileMenuProps {
  className?: string;
  onLogout?: () => void;
  appearance?: "light" | "dark";
}

export function UserProfileMenu({ className, onLogout, appearance = "light" }: UserProfileMenuProps) {
  const { user, logout } = useAuth();

  if (!user) return null;

  const handleLogout = () => {
    onLogout?.();
    logout();
  };

  return (
    <details className={cn("group relative", className)}>
      <summary
        aria-label="Open profile menu"
        className={cn(
          "flex max-w-full cursor-pointer list-none items-center gap-2 rounded-md border px-2.5 py-2 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          appearance === "dark"
            ? "border-white/20 text-white hover:bg-white/10"
            : "border-border hover:bg-muted"
        )}
      >
        <CircleUserRound className="h-5 w-5 shrink-0 text-primary" aria-hidden="true" />
        <span className="min-w-0">
          <span className={cn(
            "block max-w-[150px] truncate text-sm font-medium sm:max-w-48",
            appearance === "dark" ? "text-white" : "text-foreground"
          )}>
            {user.fullName}
          </span>
          <span className={cn(
            "block max-w-[150px] truncate text-xs sm:max-w-48",
            appearance === "dark" ? "text-white/65" : "text-muted-foreground"
          )}>
            {user.role}
          </span>
        </span>
        <ChevronDown
          className={cn(
            "h-4 w-4 shrink-0 transition-transform group-open:rotate-180",
            appearance === "dark" ? "text-white/65" : "text-muted-foreground"
          )}
          aria-hidden="true"
        />
      </summary>
      <div className="absolute right-0 top-full z-50 mt-2 w-72 max-w-[calc(100vw-1.5rem)] rounded-lg border border-border bg-card p-4 text-card-foreground shadow-lg">
        <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Profile</p>
        <p className="mt-2 break-words text-sm font-semibold">{user.fullName}</p>
        <p className="mt-1 break-all text-sm text-muted-foreground">{user.email}</p>
        <p className="mt-1 break-words text-xs text-muted-foreground">{user.role}</p>
        <Button type="button" variant="outline" size="sm" className="mt-4 w-full" onClick={handleLogout}>
          <LogOut className="mr-2 h-4 w-4" />
          Sign out
        </Button>
      </div>
    </details>
  );
}
