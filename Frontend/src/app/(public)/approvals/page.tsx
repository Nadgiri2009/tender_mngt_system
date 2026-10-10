"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ClipboardCheck } from "lucide-react";

import { fetchPendingApprovals } from "@/lib/api/workflow";
import { formatCurrencyINR, formatDate } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";

import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

export default function ApprovalsPage() {
  const { isAuthenticated, user } = useAuth();

  const { data, isLoading, isError } = useQuery({
    queryKey: ["pending-approvals"],
    queryFn: fetchPendingApprovals,
    enabled: isAuthenticated,
  });

  if (!isAuthenticated) {
    return (
      <main className="mx-auto max-w-4xl px-4 py-16 text-center sm:px-6 lg:px-8">
        <p className="text-sm text-muted-foreground">Sign in to view tenders pending your approval.</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <PageHeader
        title="Pending My Approval"
        description={`Tenders currently waiting on the ${user?.role} role.`}
      />

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tender No.</TableHead>
                <TableHead>Title</TableHead>
                <TableHead>Department</TableHead>
                <TableHead className="text-right">Estimated Cost</TableHead>
                <TableHead>Current Stage</TableHead>
                <TableHead>Since</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading &&
                Array.from({ length: 4 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 6 }).map((_, j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-4 w-full" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {isError && (
                <TableRow>
                  <TableCell colSpan={6} className="py-8 text-center text-sm text-destructive">
                    Could not load pending approvals.
                  </TableCell>
                </TableRow>
              )}

              {!isLoading && !isError && data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="py-12 text-center">
                    <div className="flex flex-col items-center gap-2 text-muted-foreground">
                      <ClipboardCheck className="h-8 w-8" />
                      <p className="text-sm">Nothing waiting on your approval right now.</p>
                    </div>
                  </TableCell>
                </TableRow>
              )}

              {data?.map((item) => (
                <TableRow key={item.tenderId}>
                  <TableCell className="font-medium">
                    <Link href={`/tenders/${item.tenderId}`} className="text-primary hover:underline">
                      {item.tenderNumber}
                    </Link>
                  </TableCell>
                  <TableCell className="max-w-xs truncate">{item.title}</TableCell>
                  <TableCell>{item.departmentName}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatCurrencyINR(item.estimatedAmount)}
                  </TableCell>
                  <TableCell>
                    <StatusBadge status={item.currentStatus} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">{formatDate(item.createdDate)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </main>
  );
}
