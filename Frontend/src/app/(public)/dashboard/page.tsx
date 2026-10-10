"use client";

import Link from "next/link";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  ArrowRight,
  BriefcaseBusiness,
  CheckCircle2,
  ClipboardCheck,
  FileCheck2,
  FileText,
  Plus,
  Send,
} from "lucide-react";
import { useQuery } from "@tanstack/react-query";

import { useAuth } from "@/contexts/AuthContext";
import { useRequireRole } from "@/hooks/useRequireRole";
import { fetchPendingApprovals, fetchRoleDashboardStats } from "@/lib/api/workflow";
import { ROLE_NAMES } from "@/types/auth";
import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

const ROLE_DASHBOARDS: Record<string, {
  title: string;
  description: string;
  focus: string;
  primaryHref: string;
  primaryLabel: string;
  primaryIcon: typeof Plus;
}> = {
  [ROLE_NAMES.DepartmentOfficer]: {
    title: "Department Dashboard",
    description: "Prepare, submit, and monitor tenders owned by your department.",
    focus: "Department work queue",
    primaryHref: "/tenders/new",
    primaryLabel: "New Tender",
    primaryIcon: Plus,
  },
  [ROLE_NAMES.Contractor]: {
    title: "Contractor Dashboard",
    description: "Track your tender participation and submitted work.",
    focus: "Contractor activity",
    primaryHref: "/tenders",
    primaryLabel: "Browse Tenders",
    primaryIcon: BriefcaseBusiness,
  },
  [ROLE_NAMES.TenderSection]: {
    title: "Tender Section Dashboard",
    description: "Manage tender processing, evaluation, and publication stages.",
    focus: "Tender processing queue",
    primaryHref: "/approvals",
    primaryLabel: "Open Work Queue",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.TenderCommittee]: {
    title: "Tender Committee Dashboard",
    description: "Review committee actions and monitor tender progress.",
    focus: "Committee review queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Tenders",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.TechnicalSanctionOfficer]: {
    title: "Technical Sanction Dashboard",
    description: "Review tenders waiting for technical sanction.",
    focus: "Technical review queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.GBOfficer]: {
    title: "GB Officer Dashboard",
    description: "Review tenders waiting for GB approval.",
    focus: "GB approval queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.DPDCOfficer]: {
    title: "DPDC Dashboard",
    description: "Review tenders waiting for administrative approval.",
    focus: "Administrative review queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.CAFOOfficer]: {
    title: "CAFO Dashboard",
    description: "Review finance-related tender decisions and approvals.",
    focus: "Finance approval queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.MCAOfficer]: {
    title: "MCA Dashboard",
    description: "Review tenders requiring MCA opinion.",
    focus: "MCA review queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
  [ROLE_NAMES.CompetentAuthority]: {
    title: "Competent Authority Dashboard",
    description: "Review tenders requiring competent authority decisions.",
    focus: "Authority approval queue",
    primaryHref: "/approvals",
    primaryLabel: "Review Approvals",
    primaryIcon: ClipboardCheck,
  },
};

const MINI_CARDS = [
  { key: "totalTenders", label: "All Tenders", icon: FileText },
  { key: "publishedTenders", label: "Published", icon: Send },
  { key: "pendingApprovals", label: "Needs Action", icon: ClipboardCheck },
  { key: "completedTenders", label: "Completed", icon: CheckCircle2 },
] as const;

export default function RoleDashboardPage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const { isLoading: roleLoading, isAuthorized } = useRequireRole(Object.values(ROLE_NAMES));
  const roleDashboard = ROLE_DASHBOARDS[user?.role ?? ""] ?? ROLE_DASHBOARDS[ROLE_NAMES.TenderSection]!;

  useEffect(() => {
    if (!authLoading && isAuthenticated && user?.role === ROLE_NAMES.Admin) {
      router.replace("/admin");
    }
  }, [authLoading, isAuthenticated, router, user?.role]);

  const { data: stats, isLoading: statsLoading, isError: statsError } = useQuery({
    queryKey: ["role-dashboard-stats"],
    queryFn: fetchRoleDashboardStats,
    enabled: isAuthenticated && user?.role !== ROLE_NAMES.Admin,
  });

  const { data: pendingApprovals, isLoading: approvalsLoading } = useQuery({
    queryKey: ["pending-approvals"],
    queryFn: fetchPendingApprovals,
    enabled: isAuthenticated && user?.role !== ROLE_NAMES.Admin,
  });

  if (authLoading || roleLoading || !isAuthorized || !isAuthenticated || !user || user.role === ROLE_NAMES.Admin) {
    return <main className="mx-auto max-w-6xl px-4 py-8"><Skeleton className="h-32 w-full" /></main>;
  }

  const PrimaryIcon = roleDashboard.primaryIcon;

  return (
    <main className="mx-auto max-w-6xl px-3 py-5 sm:px-6 sm:py-8 lg:px-8">
      <PageHeader
        title={roleDashboard.title}
        description={`${roleDashboard.description} Signed in as ${user.fullName}.`}
        actions={
          <Button asChild size="sm">
            <Link href={roleDashboard.primaryHref}><PrimaryIcon className="mr-1 h-4 w-4" />{roleDashboard.primaryLabel}</Link>
          </Button>
        }
      />

      <section aria-label="Dashboard summary" className="grid grid-cols-2 gap-2.5 sm:grid-cols-4 sm:gap-4">
        {MINI_CARDS.map((card) => {
          const Icon = card.icon;
          const value = stats?.[card.key];
          return (
            <Card key={card.key} className="min-w-0">
              <CardContent className="flex items-center gap-2.5 p-3 sm:gap-3 sm:p-4">
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary sm:h-9 sm:w-9">
                  <Icon className="h-4 w-4" />
                </div>
                <div className="min-w-0">
                  {statsLoading ? <Skeleton className="h-6 w-8" /> : <p className="text-xl font-bold tabular-nums sm:text-2xl">{value ?? "—"}</p>}
                  <p className="truncate text-[11px] text-muted-foreground sm:text-xs">{card.label}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </section>

      <div className="mt-5 grid gap-4 lg:grid-cols-[1.4fr_1fr]">
        <Card>
          <CardContent className="p-4 sm:p-5">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{roleDashboard.focus}</p>
                <h2 className="mt-1 text-lg font-semibold">Pending actions</h2>
              </div>
              <ClipboardCheck className="h-5 w-5 text-primary" />
            </div>
            {approvalsLoading && <Skeleton className="mt-4 h-20 w-full" />}
            {!approvalsLoading && pendingApprovals?.length === 0 && (
              <p className="mt-5 text-sm text-muted-foreground">No tenders are waiting for your role.</p>
            )}
            {!approvalsLoading && pendingApprovals && pendingApprovals.length > 0 && (
              <div className="mt-4 space-y-2">
                {pendingApprovals.slice(0, 4).map((item) => (
                  <Link key={item.tenderId} href={`/tenders/${item.tenderId}`} className="flex min-w-0 items-center justify-between gap-3 rounded-md border p-3 hover:bg-muted/40">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{item.tenderNumber} · {item.title}</p>
                      <p className="truncate text-xs text-muted-foreground">{item.departmentName}</p>
                    </div>
                    <ArrowRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                  </Link>
                ))}
              </div>
            )}
            <Button asChild variant="outline" size="sm" className="mt-4">
              <Link href="/approvals">View work queue <ArrowRight className="ml-1 h-4 w-4" /></Link>
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4 sm:p-5">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Your activity</p>
            <div className="mt-4 grid grid-cols-2 gap-2">
              <div className="rounded-md bg-muted/40 p-3"><p className="text-xl font-bold">{stats?.myTenders ?? "—"}</p><p className="text-xs text-muted-foreground">My tenders</p></div>
              <div className="rounded-md bg-muted/40 p-3"><p className="text-xl font-bold">{pendingApprovals?.length ?? "—"}</p><p className="text-xs text-muted-foreground">Open actions</p></div>
            </div>
            {statsError && <p className="mt-3 text-xs text-destructive">Could not load live dashboard counts.</p>}
            <div className="mt-4 flex flex-wrap gap-2">
              <Button asChild variant="outline" size="sm"><Link href="/tenders"><FileCheck2 className="mr-1 h-4 w-4" />All tenders</Link></Button>
              {user.role === ROLE_NAMES.DepartmentOfficer && <Button asChild variant="outline" size="sm"><Link href="/tenders/new"><Plus className="mr-1 h-4 w-4" />Create</Link></Button>}
            </div>
          </CardContent>
        </Card>
      </div>

      {pendingApprovals && pendingApprovals.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {pendingApprovals.slice(0, 4).map((item) => <StatusBadge key={item.tenderId} status={item.currentStatus} />)}
        </div>
      )}
    </main>
  );
}