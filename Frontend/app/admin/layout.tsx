"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import {
  ArrowLeftCircle,
  BadgeCheck,
  Building2,
  ClipboardList,
  FileText,
  Files,
  Gavel,
  List,
  LayoutDashboard,
  Megaphone,
  Menu,
  Ruler,
  ShieldCheck,
  Tags,
  X,
  Users,
} from "lucide-react";

import { useRequireRole } from "@/hooks/useRequireRole";
import { ROLE_NAMES } from "@/types/auth";
import { cn } from "@/lib/utils";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { UserProfileMenu } from "@/components/layout/UserProfileMenu";

const ADMIN_GROUPS = [
  {
    label: "Tender Management",
    icon: FileText,
    items: [
      { href: "/tenders/new", label: "Create Tender", icon: FileText },
      { href: "/tenders", label: "Tender List", icon: List },
    ],
  },
  {
    label: "Master Data",
    icon: Building2,
    items: [
      { href: "/admin/departments", label: "Departments", icon: Building2 },
      { href: "/admin/master-data/designations", label: "Designation", icon: BadgeCheck },
      { href: "/admin/users", label: "Users", icon: Users },
      { href: "/admin/roles", label: "Roles & Permissions", icon: ShieldCheck },
      { href: "/admin/master-data/tender-methods", label: "Tender Methods", icon: Gavel },
      { href: "/admin/master-data/tender-types", label: "Tender Types", icon: Tags },
      { href: "/admin/master-data/document-types", label: "Document Types", icon: Files },
      { href: "/admin/master-data/units", label: "Units of Measurement", icon: Ruler },
    ],
  },
  {
    label: "System Management",
    icon: Megaphone,
    items: [
      { href: "/admin/announcements", label: "Announcements", icon: ClipboardList },
    ],
  },
];

function NavigationGroups({
  pathname,
  onNavigate,
}: {
  pathname: string | null;
  onNavigate?: () => void;
}) {
  return (
    <>
      <Link
        href="/admin"
        onClick={onNavigate}
        className={cn(
          "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-white/10 hover:text-white",
          pathname === "/admin" ? "bg-white/10 text-white" : "text-white/70"
        )}
      >
        <LayoutDashboard className="h-4 w-4" />
        Dashboard
      </Link>
      {ADMIN_GROUPS.map((group) => (
        <details key={group.label} open className="group rounded-md">
          <summary className="flex cursor-pointer list-none items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-white/70 hover:bg-white/10 hover:text-white">
            <group.icon className="h-4 w-4" />
            <span className="flex-1">{group.label}</span>
            <span aria-hidden="true" className="text-xs transition-transform group-open:rotate-90">›</span>
          </summary>
          <div className="ml-3 border-l border-white/15 py-1 pl-3">
            {group.items.map((item) => {
              const isActive = pathname === item.href ||
                (item.href !== "/tenders" && pathname?.startsWith(`${item.href}/`));
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={onNavigate}
                  className={cn(
                    "flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors hover:bg-white/10 hover:text-white",
                    isActive ? "bg-white/10 font-medium text-white" : "text-white/65"
                  )}
                  aria-current={isActive ? "page" : undefined}
                >
                  <item.icon className="h-4 w-4 shrink-0" />
                  {item.label}
                </Link>
              );
            })}
          </div>
        </details>
      ))}
    </>
  );
}

export default function AdminLayout({ children }: { children: ReactNode }) {
  const { isLoading, isAuthorized } = useRequireRole([ROLE_NAMES.JuniorEngineer]);
  const pathname = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Skeleton className="h-8 w-48" />
      </div>
    );
  }

  if (!isAuthorized) return null;

  return (
    <div className="flex min-h-screen flex-col sm:flex-row">
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col border-r border-border bg-[hsl(var(--navy-950))] text-white sm:flex">
        <div className="flex items-center gap-2 px-5 py-5">
          <Building2 className="h-6 w-6" />
          <div className="leading-tight">
            <p className="text-sm font-semibold">Junior Engineer (JE) Main Admin</p>
            <p className="text-xs text-white/60">Tender Management</p>
          </div>
        </div>
        <nav aria-label="Main navigation" className="flex-1 space-y-2 overflow-y-auto px-3">
          <NavigationGroups pathname={pathname} />
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

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 border-b border-border bg-[hsl(var(--navy-950))] px-3 py-2 text-white sm:hidden">
          <div className="flex min-w-0 items-center gap-3">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="shrink-0 text-white hover:bg-white/10 hover:text-white"
              aria-label={menuOpen ? "Close navigation menu" : "Open navigation menu"}
              aria-expanded={menuOpen}
              aria-controls="Junior Engineer (JE)-mobile-navigation"
              onClick={() => setMenuOpen((open) => !open)}
            >
              {menuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </Button>
            <div className="min-w-0 leading-tight">
              <p className="truncate text-sm font-semibold">Junior Engineer (JE) Main Admin</p>
              <p className="truncate text-xs text-white/60">Tender Management</p>
            </div>
            <UserProfileMenu className="ml-auto" appearance="dark" />
          </div>
          {menuOpen && (
            <nav id="Junior Engineer (JE)-mobile-navigation" aria-label="Main navigation" className="max-h-[75vh] space-y-2 overflow-y-auto py-3">
              <NavigationGroups pathname={pathname} onNavigate={() => setMenuOpen(false)} />
              <Link
                href="/"
                onClick={() => setMenuOpen(false)}
                className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-white/70 hover:bg-white/10 hover:text-white"
              >
                <ArrowLeftCircle className="h-4 w-4" /> Back to Portal
              </Link>
            </nav>
          )}
        </header>

        <header className="sticky top-0 z-30 hidden h-16 items-center justify-end border-b border-border bg-background/95 px-6 backdrop-blur sm:flex">
          <UserProfileMenu />
        </header>

        <main className="min-w-0 flex-1 bg-background">
          <div className="mx-auto w-full max-w-6xl px-3 py-6 sm:px-6 lg:px-8">{children}</div>
        </main>
      </div>
    </div>
  );
}
