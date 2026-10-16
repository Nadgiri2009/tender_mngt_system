"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { Menu, X, LogOut, LayoutDashboard, ClipboardCheck } from "lucide-react";

import { useAuth } from "@/contexts/AuthContext";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const NAV_LINKS = [
  { href: "/", label: "Home" },
  { href: "/tenders", label: "Tenders" },
  { href: "/terms", label: "Terms & Conditions" },
  { href: "/#departments", label: "Departments" },
  { href: "/#announcements", label: "Notices" },
  { href: "/#contact", label: "Contact" },
];

export function Navbar() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const { user, isAuthenticated, logout } = useAuth();
  const pathname = usePathname();

  const handleLogout = () => {
    setMobileOpen(false);
    logout();
  };

  return (
    <header className="sticky top-0 z-40 w-full border-b border-border bg-card/95 backdrop-blur">
      <div className="bg-primary py-1.5 text-center text-xs font-medium text-primary-foreground">
        Government of Maharashtra &middot; Solapur Municipal Corporation &middot; Official e-Tender Portal
      </div>
      <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <Link href="/" className="flex items-center gap-2.5">
          <Image
            src="/smc.png"
            alt="Solapur Municipal Corporation seal"
            width={40}
            height={40}
            priority
            className="h-10 w-10 rounded-full bg-white object-contain"
          />
          <div className="leading-tight">
            <p className="text-sm font-bold text-foreground sm:text-base">Solapur Municipal Corporation</p>
            <p className="text-xs text-muted-foreground">Tender Management System</p>
          </div>
        </Link>

        <nav className="hidden items-center gap-6 lg:flex">
          {NAV_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={cn(
                "text-sm font-medium text-muted-foreground transition-colors hover:text-primary",
                pathname === link.href && "text-primary"
              )}
            >
              {link.label}
            </Link>
          ))}
        </nav>

        <div className="hidden items-center gap-3 lg:flex">
          {isAuthenticated ? (
            <>
              {pathname !== "/" && user?.role !== "Contractor" && (
                <Button asChild variant="outline" size="sm">
                  <Link href="/approvals">
                    <ClipboardCheck className="mr-1 h-4 w-4" /> My Approvals
                  </Link>
                </Button>
              )}
              {user?.role !== "Admin" && (
                <Button asChild variant="outline" size="sm">
                  <Link href="/dashboard">
                    <LayoutDashboard className="mr-1 h-4 w-4" /> Dashboard
                  </Link>
                </Button>
              )}
              {user?.role === "Admin" && (
                <Button asChild variant="outline" size="sm">
                  <Link href="/admin">
                    <LayoutDashboard className="mr-1 h-4 w-4" /> Admin Panel
                  </Link>
                </Button>
              )}
              <span className="text-sm text-muted-foreground">
                {user?.fullName} <span className="text-xs">({user?.role})</span>
              </span>
              <Button variant="ghost" size="sm" onClick={handleLogout}>
                <LogOut className="mr-1 h-4 w-4" /> Logout
              </Button>
            </>
          ) : (
            <>
              <Button asChild variant="outline" size="sm">
                <Link href="/register">Register</Link>
              </Button>
              <Button asChild size="sm">
                <Link href="/login">Login</Link>
              </Button>
            </>
          )}
        </div>

        <button
          className="lg:hidden"
          onClick={() => setMobileOpen((v) => !v)}
          aria-label={mobileOpen ? "Close menu" : "Open menu"}
        >
          {mobileOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
        </button>
      </div>

      {mobileOpen && (
        <div className="border-t border-border bg-card px-4 py-4 lg:hidden">
          <nav className="flex flex-col gap-3">
            {NAV_LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                onClick={() => setMobileOpen(false)}
                className="text-sm font-medium text-muted-foreground hover:text-primary"
              >
                {link.label}
              </Link>
            ))}
            <div className="mt-2 flex flex-col gap-2 border-t border-border pt-3">
              {isAuthenticated ? (
                <>
                  {pathname !== "/" && user?.role !== "Contractor" && (
                    <Button asChild variant="outline" size="sm">
                      <Link href="/approvals" onClick={() => setMobileOpen(false)}>
                        My Approvals
                      </Link>
                    </Button>
                  )}
                  {user?.role !== "Admin" && (
                    <Button asChild variant="outline" size="sm">
                      <Link href="/dashboard" onClick={() => setMobileOpen(false)}>Dashboard</Link>
                    </Button>
                  )}
                  {user?.role === "Admin" && (
                    <Button asChild variant="outline" size="sm">
                      <Link href="/admin" onClick={() => setMobileOpen(false)}>
                        Admin Panel
                      </Link>
                    </Button>
                  )}
                  <Button variant="ghost" size="sm" onClick={handleLogout}>
                    Logout ({user?.fullName})
                  </Button>
                </>
              ) : (
                <>
                  <Button asChild variant="outline" size="sm">
                    <Link href="/register" onClick={() => setMobileOpen(false)}>
                      Register
                    </Link>
                  </Button>
                  <Button asChild size="sm">
                    <Link href="/login" onClick={() => setMobileOpen(false)}>
                      Login
                    </Link>
                  </Button>
                </>
              )}
            </div>
          </nav>
        </div>
      )}
    </header>
  );
}
