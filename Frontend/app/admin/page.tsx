"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { FileText, Building2, Users, BarChart3, Eye, CheckCircle2, Clock3, XCircle, SearchCheck, CalendarClock, Layers3 } from "lucide-react";

import { fetchAdminStats } from "@/lib/api/dashboard";
import { fetchTenders } from "@/lib/api/tenders";
import { formatDate } from "@/lib/utils";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/tenders/StatusBadge";
import { Skeleton } from "@/components/ui/skeleton";

export default function AdminDashboardPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-stats"],
    queryFn: fetchAdminStats,
  });

  const { data: tenderData, isLoading: isLoadingTenders } = useQuery({
    queryKey: ["admin-dashboard-tenders"],
    queryFn: () =>
      fetchTenders({
        pageNumber: 1,
        pageSize: 10,
        sortBy: "createdDate",
        sortDescending: true,
      }),
  });

  return (
    <div>
      <PageHeader
        title="Admin Dashboard"
        description="Live system overview."
        actions={
          <Button asChild>
            <Link href="/tenders/new">Create Tender</Link>
          </Button>
        }
      />

      {isError && <p className="text-sm text-destructive">Could not load dashboard stats.</p>}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { label: "Total Tenders", value: data?.totalTenders, icon: FileText },
          { label: "Draft", value: data?.draftTenders, icon: FileText },
          { label: "Approved", value: data?.approvedTenders, icon: CheckCircle2 },
          { label: "Published", value: data?.publishedTenders, icon: CheckCircle2 },
          { label: "Pending", value: data?.pendingTenders, icon: Clock3 },
          { label: "Rejected", value: data?.rejectedTenders, icon: XCircle },
          { label: "Under Review", value: data?.underReviewTenders, icon: SearchCheck },
          { label: "Closed / Expired", value: data?.closedExpiredTenders, icon: CalendarClock },
          { label: "Departments", value: data?.totalDepartments, icon: Building2 },
          { label: "Staff & Vendor Users", value: data?.totalUsers, icon: Users },
        ].map((card) => (
          <Card key={card.label}>
            <CardContent className="flex items-center gap-4 pt-6">
              <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                <card.icon className="h-5 w-5" />
              </div>
              <div>
                {isLoading ? (
                  <Skeleton className="h-7 w-10" />
                ) : (
                  <p className="text-2xl font-bold text-foreground">{card.value ?? "—"}</p>
                )}
                <p className="text-xs text-muted-foreground">{card.label}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <FileText className="h-4 w-4" /> Recent Tenders
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading && <Skeleton className="h-32 w-full" />}
            {!isLoading && !data?.recentTenders.length && (
              <p className="text-sm text-muted-foreground">No recent tenders.</p>
            )}
            {!!data?.recentTenders.length && (
              <ol aria-label="Recent tenders" className="space-y-2">
                {data.recentTenders.map((tender, index) => (
                  <li key={tender.tenderId} className="flex min-w-0 items-center gap-3 rounded-md border p-3">
                    <span className="w-7 shrink-0 text-center text-sm font-semibold tabular-nums text-muted-foreground">{index + 1}</span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{tender.tenderNumber} · {tender.title}</p>
                      <p className="truncate text-xs text-muted-foreground">
                        {tender.tenderType} · {tender.departmentName} · RESP Officer: {tender.assignedRespOfficer || "Unassigned"} · Assigned role: {tender.assignedRole || "No pending role"}
                      </p>
                      <div className="mt-1"><StatusBadge status={tender.currentStatus} /></div>
                    </div>
                    <Button asChild variant="outline" size="sm" className="shrink-0">
                      <Link href={`/tenders/${tender.tenderId}`}><Eye className="mr-1 h-4 w-4" />View</Link>
                    </Button>
                  </li>
                ))}
              </ol>
            )}
            <div className="mt-4 flex justify-end">
              <Button asChild variant="outline" size="sm">
                <Link href="/tenders">View all tenders <Eye className="ml-1 h-4 w-4" /></Link>
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <CalendarClock className="h-4 w-4" /> Upcoming Submission Deadlines
            </CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading && <Skeleton className="h-32 w-full" />}
            {!isLoading && !data?.upcomingDeadlines.length && (
              <p className="text-sm text-muted-foreground">No upcoming deadlines.</p>
            )}
            {!!data?.upcomingDeadlines.length && (
              <ol aria-label="Upcoming submission deadlines" className="space-y-2">
                {data.upcomingDeadlines.map((tender, index) => (
                  <li key={tender.tenderId} className="flex min-w-0 items-center gap-3 rounded-md border p-3">
                    <span className="w-7 shrink-0 text-center text-sm font-semibold tabular-nums text-muted-foreground">{index + 1}</span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{tender.tenderNumber} · {tender.title}</p>
                      <p className="truncate text-xs text-muted-foreground">
                        {tender.tenderType} · {tender.departmentName} · RESP Officer: {tender.assignedRespOfficer || "Unassigned"} · Assigned role: {tender.assignedRole || "No pending role"}
                      </p>
                    </div>
                    <div className="flex shrink-0 flex-col items-end gap-2 sm:flex-row sm:items-center">
                      <span className="whitespace-nowrap text-xs font-medium">
                        {tender.submissionDeadline ? formatDate(tender.submissionDeadline) : "—"}
                      </span>
                      <Button asChild variant="outline" size="sm">
                        <Link href={`/tenders/${tender.tenderId}`}><Eye className="mr-1 h-4 w-4" />View</Link>
                      </Button>
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </CardContent>
        </Card>
      </div>

      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Layers3 className="h-4 w-4" /> Tenders by Category
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!isLoading && data?.categoryBreakdown.length === 0 && (
            <p className="text-sm text-muted-foreground">No tenders yet.</p>
          )}
          <div className="space-y-2">
            {data?.categoryBreakdown.map((row) => (
              <div key={row.category} className="flex items-center justify-between rounded-md bg-muted/40 px-3 py-2 text-sm">
                <span>{row.category}</span><span className="font-semibold tabular-nums">{row.tenderCount}</span>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <BarChart3 className="h-4 w-4" /> Tenders by Status
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading && <Skeleton className="h-40 w-full" />}
          {!isLoading && data && data.statusBreakdown.length === 0 && (
            <p className="text-sm text-muted-foreground">No tenders yet.</p>
          )}
          {!isLoading && data && data.statusBreakdown.length > 0 && (
            <div className="space-y-2">
              {data.statusBreakdown.map((row) => {
                const max = Math.max(...data.statusBreakdown.map((r) => r.count), 1);
                return (
                  <div key={row.status} className="flex items-center gap-3">
                    <span className="w-48 shrink-0 truncate text-sm text-muted-foreground">{row.status}</span>
                    <div className="h-2 flex-1 rounded-full bg-secondary">
                      <div
                        className="h-2 rounded-full bg-primary"
                        style={{ width: `${(row.count / max) * 100}%` }}
                      />
                    </div>
                    <span className="w-8 text-right text-sm font-medium text-foreground">{row.count}</span>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <FileText className="h-4 w-4" /> All Tenders
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoadingTenders ? (
            <div className="p-6"><Skeleton className="h-40 w-full" /></div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>S.No</TableHead>
                    <TableHead>Tender ID</TableHead>
                    <TableHead>Tender Title</TableHead>
                    <TableHead>Department</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Pending Stage</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead>Last Updated</TableHead>
                    <TableHead className="text-right">Action</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {!tenderData?.items.length && (
                    <TableRow>
                      <TableCell colSpan={9} className="py-12 text-center text-sm text-muted-foreground">
                        No tenders found.
                      </TableCell>
                    </TableRow>
                  )}
                  {tenderData?.items.map((tender, index) => (
                    <TableRow key={tender.tenderId}>
                      <TableCell>{index + 1}</TableCell>
                      <TableCell className="font-medium">{tender.tenderNumber}</TableCell>
                      <TableCell>{tender.title}</TableCell>
                      <TableCell>{tender.departmentName || "—"}</TableCell>
                      <TableCell><StatusBadge status={tender.currentStatus} /></TableCell>
                      <TableCell>{tender.currentStatus}</TableCell>
                      <TableCell>{formatDate(tender.createdDate)}</TableCell>
                      <TableCell>{formatDate(tender.createdDate)}</TableCell>
                      <TableCell className="text-right">
                        <Button asChild variant="outline" size="sm">
                          <Link href={`/tenders/${tender.tenderId}`}>
                            <Eye className="mr-1 h-4 w-4" /> View
                          </Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
