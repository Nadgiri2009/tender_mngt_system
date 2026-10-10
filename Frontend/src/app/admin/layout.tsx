"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Building2,
  Megaphone,
  Users,
  ArrowLeftCircle,
  FileText,
} from "lucide-react";

import { useRequireRole } from "@/hooks/useRequireRole";
import { ROLE_NAMES } from "@/types/auth";
import { cn } from "@/lib/utils";
import { Skeleton } from "@/components/ui/skeleton";

const ADMIN_NAV = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/tenders", label: "Tenders", icon: FileText },
  { href: "/admin/departments", label: "Departments", icon: Building2 },
  { href: "/admin/announcements", label: "Announcements", icon: Megaphone },
  { href: "/admin/users", label: "Staff Users", icon: Users },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const { isLoading, isAuthorized } = useRequireRole([ROLE_NAMES.Admin]);
  const pathname = usePathname();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Skeleton className="h-8 w-48" />
      </div>
    );
  }

  if (!isAuthorized) {
    return null; // redirect is in-flight via useRequireRole
  }

  return (
    <div className="flex min-h-screen flex-col sm:flex-row">
      <aside className="hidden w-64 shrink-0 flex-col border-r border-border bg-[hsl(var(--navy-950))] text-white sm:flex">
        <div className="flex items-center gap-2 px-5 py-5">
          <Building2 className="h-6 w-6" />
          <div className="leading-tight">
            <p className="text-sm font-semibold">SMC Admin Panel</p>
            <p className="text-xs text-white/60">Tender Management</p>
          </div>
        </div>
        <nav className="flex-1 space-y-1 px-3">
          {ADMIN_NAV.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-white/70 transition-colors hover:bg-white/10 hover:text-white",
                pathname === item.href && "bg-white/10 text-white"
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </Link>
          ))}
        </nav>
        <div className="border-t border-white/10 px-3 py-4">
          <Link
            href="/"
            className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-white/70 hover:bg-white/10 hover:text-white"
          >
            <ArrowLeftCircle className="h-4 w-4" /> Back to Portal
          </Link>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <nav className="border-b border-border bg-[hsl(var(--navy-950))] px-3 py-3 text-white sm:hidden">
          <div className="flex gap-2 overflow-x-auto pb-1">
            {ADMIN_NAV.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-xs font-medium whitespace-nowrap transition-colors",
                  pathname === item.href
                    ? "bg-white/10 text-white"
                    : "text-white/70 hover:bg-white/10 hover:text-white"
                )}
              >
                <item.icon className="h-3.5 w-3.5" />
                {item.label}
              </Link>
            ))}
            <Link
              href="/"
              className="flex shrink-0 items-center gap-2 rounded-md px-3 py-2 text-xs font-medium whitespace-nowrap text-white/70 hover:bg-white/10 hover:text-white"
            >
              <ArrowLeftCircle className="h-3.5 w-3.5" /> Portal
            </Link>
          </div>
        </nav>

        <main className="flex-1 bg-background">
          <div className="mx-auto max-w-6xl px-3 py-6 sm:px-6 lg:px-8">{children}</div>
        </main>
      </div>
    </div>
  );
}
