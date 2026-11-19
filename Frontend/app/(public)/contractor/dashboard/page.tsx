"use client";

import Link from "next/link";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { useAuth } from "@/contexts/AuthContext";
import { fetchPublicTenders } from "@/lib/api/tenders";
import { getContractorApplicationDashboard, markContractorNotificationRead } from "@/lib/api/applications";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyINR, formatDate } from "@/lib/utils";
import { ROLE_NAMES } from "@/types/auth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

const summaryLabels = [
  ["totalApplications", "Total Applications"],
  ["draftApplications", "Drafts"],
  ["submittedApplications", "Submitted"],
  ["underVerificationApplications", "Under Verification"],
  ["shortfallApplications", "Clarifications"],
  ["approvedApplications", "Approved"],
] as const;

export default function ContractorDashboardPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user, isLoading: authLoading } = useAuth();

  useEffect(() => {
    if (!authLoading && !user) {
      router.replace(`/login?returnUrl=${encodeURIComponent("/contractor/dashboard")}`);
    }
  }, [authLoading, router, user]);

  const dashboardQuery = useQuery({
    queryKey: ["contractor-application-dashboard"],
    queryFn: getContractorApplicationDashboard,
    enabled: user?.role === ROLE_NAMES.ExternalBidder,
  });
  const tendersQuery = useQuery({
    queryKey: ["tenders", "contractor-available"],
    queryFn: () => fetchPublicTenders({ pageNumber: 1, pageSize: 10, sortBy: "createdDate", sortDescending: true }),
  });
  const notificationMutation = useMutation({
    mutationFn: markContractorNotificationRead,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["contractor-application-dashboard"] });
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });

  if (authLoading || !user) {
    return <main className="mx-auto max-w-7xl px-4 py-8"><Skeleton className="h-64 w-full" /></main>;
  }
  if (user.role !== ROLE_NAMES.ExternalBidder) {
    return (
      <main className="mx-auto max-w-4xl px-4 py-8">
        <Card><CardHeader><CardTitle>Contractor account required</CardTitle></CardHeader>
          <CardContent><p className="text-sm text-muted-foreground">This dashboard is available to registered contractor accounts.</p></CardContent>
        </Card>
      </main>
    );
  }

  const dashboard = dashboardQuery.data;

  return (
    <main className="mx-auto max-w-7xl space-y-8 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold">Contractor Dashboard</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {dashboard ? `${dashboard.contractorName} · ${dashboard.companyName}` : "Your applications, documents, and tender updates."}
          </p>
        </div>
        <Button asChild variant="outline"><Link href="/profile">Contractor Profile</Link></Button>
      </div>

      {dashboardQuery.isError && <p role="alert" className="text-sm text-destructive">Could not load your contractor data.</p>}
      {dashboardQuery.isLoading ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => <Skeleton className="h-24" key={index} />)}
        </div>
      ) : dashboard && (
        <>
          <section aria-label="Application overview" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {summaryLabels.map(([key, label]) => (
              <Card key={key}>
                <CardContent className="p-4">
                  <p className="text-sm text-muted-foreground">{label}</p>
                  <p className="mt-1 text-2xl font-semibold tabular-nums">{dashboard[key]}</p>
                </CardContent>
              </Card>
            ))}
          </section>

          <section>
            <div className="mb-3 flex items-center justify-between gap-3">
              <h2 className="text-lg font-semibold">My Applications</h2>
              <Button asChild variant="outline" size="sm"><Link href="/tenders">Browse Tenders</Link></Button>
            </div>
            <Card>
              <CardContent className="p-0">
                {dashboard.applications.length === 0 ? (
                  <p className="p-6 text-sm text-muted-foreground">You have not started an application yet.</p>
                ) : (
                  <div className="divide-y">
                    {dashboard.applications.map((application) => (
                      <div key={application.technicalBidId} className="flex flex-wrap items-center justify-between gap-3 p-4">
                        <div className="min-w-0">
                          <p className="font-medium">{application.tenderNumber} · {application.tenderTitle}</p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            {application.applicationReference} · {application.submittedAtUtc ? formatDate(application.submittedAtUtc) : "Draft"}
                          </p>
                          {application.pendingMandatoryDocumentCount > 0 && (
                            <p className="mt-1 text-xs text-destructive">
                              {application.pendingMandatoryDocumentCount} required document(s) pending
                            </p>
                          )}
                        </div>
                        <div className="flex items-center gap-2">
                          <StatusBadge status={application.status} />
                          <Button asChild size="sm" variant="outline">
                            <Link href={application.status === "Draft"
                              ? `/tenders/${application.tenderId}/apply`
                              : `/contractor/applications/${application.technicalBidId}`}>
                              {application.status === "Draft" ? "Continue" : "View"}
                            </Link>
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </section>
        </>
      )}

      {dashboard && (
        <section>
          <h2 className="mb-3 text-lg font-semibold">Notifications</h2>
          <Card>
            <CardContent className="divide-y p-0">
              {dashboard.notifications.length === 0 ? (
                <p className="p-5 text-sm text-muted-foreground">No notifications.</p>
              ) : dashboard.notifications.map((notification) => (
                <div key={notification.notificationId} className="flex flex-wrap items-center justify-between gap-3 p-4">
                  <div>
                    <p className={`text-sm ${notification.readAtUtc ? "text-muted-foreground" : "font-medium"}`}>
                      {notification.tenderNumber}: {notification.message}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">{formatDate(notification.createdAtUtc)}</p>
                  </div>
                  {!notification.readAtUtc && (
                    <Button size="sm" variant="outline" disabled={notificationMutation.isPending}
                      onClick={() => notificationMutation.mutate(notification.notificationId)}>Mark as read</Button>
                  )}
                </div>
              ))}
            </CardContent>
          </Card>
        </section>
      )}

      <section>
        <div className="mb-3 flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">Available Tenders</h2>
          <Button asChild variant="outline" size="sm"><Link href="/tenders">View All</Link></Button>
        </div>
        {tendersQuery.isError && <p role="alert" className="text-sm text-destructive">Could not load published tenders.</p>}
        {tendersQuery.isLoading ? <Skeleton className="h-40 w-full" /> : (
          <div className="grid gap-3 md:grid-cols-2">
            {tendersQuery.data?.items.map((tender) => (
              <Card key={tender.tenderId}>
                <CardContent className="flex flex-wrap items-center justify-between gap-3 p-4">
                  <div className="min-w-0">
                    <p className="font-medium">{tender.tenderNumber} · {tender.title}</p>
                    <p className="mt-1 text-sm text-muted-foreground">{tender.departmentName} · {formatCurrencyINR(tender.estimatedAmount)}</p>
                    <p className="mt-1 text-xs text-muted-foreground">Deadline: {formatDate(tender.submissionDeadline)}</p>
                  </div>
                  <div className="flex gap-2">
                    <Button asChild size="sm" variant="outline"><Link href={`/tenders/${tender.tenderId}`}>View</Link></Button>
                    {tender.canApply && <Button asChild size="sm"><Link href={`/tenders/${tender.tenderId}/apply`}>Apply</Link></Button>}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
