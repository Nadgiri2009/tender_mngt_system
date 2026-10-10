"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";

import { fetchTenders } from "@/lib/api/tenders";
import { fetchDepartments } from "@/lib/api/departments";
import { fetchTenderStatuses } from "@/lib/api/lookups";
import { formatCurrencyINR, formatDate } from "@/lib/utils";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";
import { Search, ChevronLeft, ChevronRight, FileWarning } from "lucide-react";

const ALL = "__all__";

export function TenderTable() {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<string>(ALL);
  const [departmentId, setDepartmentId] = useState<string>(ALL);
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;

  const debouncedSearch = useDebouncedValue(search, 400);

  const { data: departments } = useQuery({
    queryKey: ["departments"],
    queryFn: fetchDepartments,
  });

  const { data: statuses } = useQuery({
    queryKey: ["tender-statuses"],
    queryFn: fetchTenderStatuses,
  });

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["tenders", { debouncedSearch, status, departmentId, pageNumber, pageSize }],
    queryFn: () =>
      fetchTenders({
        search: debouncedSearch,
        status: status === ALL ? undefined : status,
        departmentId: departmentId === ALL ? undefined : Number(departmentId),
        pageNumber,
        pageSize,
        sortBy: "createdDate",
        sortDescending: true,
      }),
  });

  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search by title or tender number..."
            className="pl-9"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPageNumber(1);
            }}
          />
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <Select
            value={status}
            onValueChange={(v) => {
              setStatus(v);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-full sm:w-56">
              <SelectValue placeholder="All statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All statuses</SelectItem>
              {statuses?.map((s) => (
                <SelectItem key={s} value={s}>
                  {s}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select
            value={departmentId}
            onValueChange={(v) => {
              setDepartmentId(v);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-full sm:w-56">
              <SelectValue placeholder="All departments" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All departments</SelectItem>
              {departments?.map((d) => (
                <SelectItem key={d.departmentId} value={String(d.departmentId)}>
                  {d.departmentName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border">
        <Table className="min-w-[780px]">
          <TableHeader>
            <TableRow>
              <TableHead>Tender No.</TableHead>
              <TableHead>Title</TableHead>
              <TableHead>Department</TableHead>
              <TableHead className="text-right">Estimated Cost</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Created</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
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
                <TableCell colSpan={6} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-destructive">
                    <FileWarning className="h-8 w-8" />
                    <p className="text-sm">{error instanceof Error ? error.message : "Failed to load tenders."}</p>
                  </div>
                </TableCell>
              </TableRow>
            )}

            {!isLoading && !isError && data?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="py-12 text-center text-sm text-muted-foreground">
                  No tenders match your filters.
                </TableCell>
              </TableRow>
            )}

            {!isLoading &&
              !isError &&
              data?.items.map((tender) => (
                <TableRow key={tender.tenderId}>
                  <TableCell className="font-medium">
                    <Link href={`/tenders/${tender.tenderId}`} className="text-primary hover:underline">
                      {tender.tenderNumber}
                    </Link>
                  </TableCell>
                  <TableCell className="max-w-xs truncate">{tender.title}</TableCell>
                  <TableCell>{tender.departmentName}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatCurrencyINR(tender.estimatedAmount)}
                  </TableCell>
                  <TableCell>
                    <StatusBadge status={tender.currentStatus} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">{formatDate(tender.createdDate)}</TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </div>

      {data && data.totalCount > 0 && (
        <div className="flex flex-col gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
          <p>
            Page {data.pageNumber} of {totalPages} &middot; {data.totalCount} tenders total
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            >
              <ChevronLeft className="h-4 w-4" /> Prev
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pageNumber >= totalPages}
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
            >
              Next <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
