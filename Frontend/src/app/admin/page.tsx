"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { FileText, FileClock, Building2, Users, BarChart3, Eye } from "lucide-react";

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
      <PageHeader title="Admin Dashboard" description="Live system overview." />

      {isError && <p className="text-sm text-destructive">Could not load dashboard stats.</p>}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { label: "Total Tenders", value: data?.totalTenders, icon: FileText },
          { label: "Draft Tenders", value: data?.draftTenders, icon: FileClock },
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
