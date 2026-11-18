"use client";

import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClipboardCheck, Search } from "lucide-react";
import { toast } from "sonner";

import { executeWorkflowAction, fetchPendingApprovals } from "@/lib/api/workflow";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyINR, formatDate } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";

import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { PaginationControls } from "@/components/layout/PaginationControls";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useState } from "react";

const pageSize = 10;

export default function ApprovalsPage() {
  const { isAuthenticated, user } = useAuth();
  const queryClient = useQueryClient();
  const [remarks, setRemarks] = useState<Record<number, string>>({});
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const debouncedSearch = useDebouncedValue(search, 350);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["pending-approvals", debouncedSearch, pageNumber, pageSize],
    queryFn: () => fetchPendingApprovals({ search: debouncedSearch, pageNumber, pageSize }),
    enabled: isAuthenticated,
  });

  const actionMutation = useMutation({
    mutationFn: (input: { tenderId: number; actionName: string }) =>
      executeWorkflowAction(input.tenderId, input.actionName, remarks[input.tenderId] || undefined),
    onSuccess: (result) => {
      toast.success(`Moved to ${result.newStatus}.`);
      queryClient.invalidateQueries({ queryKey: ["pending-approvals"] });
      queryClient.invalidateQueries({ queryKey: ["role-dashboard-stats"] });
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Workflow action failed."),
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

      <div className="mb-4 max-w-xl">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPageNumber(1);
            }}
            placeholder="Search assigned tenders"
            className="pl-9"
            aria-label="Search assigned tenders"
          />
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
          <Table className="min-w-[900px]">
            <TableHeader>
              <TableRow>
                <TableHead>Sr. No.</TableHead>
                <TableHead>Tender No.</TableHead>
                <TableHead>Title</TableHead>
                <TableHead>Department</TableHead>
                <TableHead className="text-right">Estimated Cost</TableHead>
                <TableHead>Current Stage</TableHead>
                <TableHead>Since</TableHead>
                <TableHead>Configured actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading &&
                Array.from({ length: 4 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 8 }).map((_, j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-4 w-full" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {isError && (
                <TableRow>
                  <TableCell colSpan={8} className="py-8 text-center text-sm text-destructive">
                    Could not load pending approvals. Refresh and try again.
                  </TableCell>
                </TableRow>
              )}

              {!isLoading && !isError && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} className="py-12 text-center">
                    <div className="flex flex-col items-center gap-2 text-muted-foreground">
                      <ClipboardCheck className="h-8 w-8" />
                      <p className="text-sm">Nothing waiting on your approval right now.</p>
                    </div>
                  </TableCell>
                </TableRow>
              )}

              {data?.items.map((item, index) => (
                <TableRow key={item.tenderId}>
                  <TableCell className="text-muted-foreground">{(pageNumber - 1) * pageSize + index + 1}</TableCell>
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
                  <TableCell className="min-w-52 space-y-2">
                    {(item.availableActions ?? []).length > 0 ? (
                      <>
                        <Textarea
                          aria-label={`Remarks for ${item.tenderNumber}`}
                          maxLength={1000}
                          placeholder="Remarks (optional)"
                          value={remarks[item.tenderId] ?? ""}
                          onChange={(event) => setRemarks((current) => ({
                            ...current,
                            [item.tenderId]: event.target.value,
                          }))}
                        />
                        <div className="flex flex-wrap gap-2">
                          {item.availableActions.map((actionName) => (
                            <Button
                              key={actionName}
                              size="sm"
                              disabled={actionMutation.isPending}
                              onClick={() => actionMutation.mutate({ tenderId: item.tenderId, actionName })}
                            >
                              {actionName.replace(/([a-z])([A-Z])/g, "$1 $2")}
                            </Button>
                          ))}
                        </div>
                      </>
                    ) : <span className="text-sm text-muted-foreground">No actions configured</span>}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          </div>
        </CardContent>
      </Card>

      {data && (
        <div className="mt-4">
          <PaginationControls
            pageNumber={data.pageNumber}
            pageSize={data.pageSize}
            totalCount={data.totalCount}
            totalPages={data.totalPages}
            onPageChange={setPageNumber}
          />
        </div>
      )}
    </main>
  );
}
