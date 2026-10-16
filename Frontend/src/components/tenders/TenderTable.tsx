"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";

import { fetchTenders } from "@/lib/api/tenders";
import { fetchDepartments } from "@/lib/api/departments";
import { fetchTenderStatuses, fetchTenderTypes } from "@/lib/api/lookups";
import { formatCurrencyINR, formatDate } from "@/lib/utils";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";
import { Search, ChevronLeft, ChevronRight, FileWarning, Eye, Send } from "lucide-react";

const ALL = "__all__";

export function TenderTable() {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<string>(ALL);
  const [departmentId, setDepartmentId] = useState<string>(ALL);
  const [tenderType, setTenderType] = useState<string>(ALL);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
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

  const { data: tenderTypes } = useQuery({
    queryKey: ["tender-types"],
    queryFn: fetchTenderTypes,
  });

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["tenders", { debouncedSearch, status, departmentId, tenderType, dateFrom, dateTo, pageNumber, pageSize }],
    queryFn: () =>
      fetchTenders({
        search: debouncedSearch,
        status: status === ALL ? undefined : status,
        departmentId: departmentId === ALL ? undefined : Number(departmentId),
        tenderType: tenderType === ALL ? undefined : tenderType,
        submissionDateFrom: dateFrom || undefined,
        submissionDateTo: dateTo || undefined,
        pageNumber,
        pageSize,
        sortBy: "createdDate",
        sortDescending: true,
      }),
  });

  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="space-y-4">
      <div className="grid min-w-0 gap-3 lg:grid-cols-[minmax(240px,1fr)_minmax(0,2fr)]">
        <div className="relative min-w-0">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search by title or tender number..."
            className="w-full pl-9"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPageNumber(1);
            }}
          />
        </div>

        <div className="grid min-w-0 gap-3 sm:grid-cols-3">
          <Select
            value={status}
            onValueChange={(v) => {
              setStatus(v);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-full min-w-0">
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
            <SelectTrigger className="w-full min-w-0">
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

          <Select
            value={tenderType}
            onValueChange={(value) => {
              setTenderType(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-full min-w-0">
              <SelectValue placeholder="All categories" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All categories</SelectItem>
              {tenderTypes?.map((type) => <SelectItem key={type} value={type}>{type}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:max-w-2xl">
        <label className="block min-w-0 space-y-1 text-sm text-muted-foreground">
          <span className="block">Submission deadline from</span>
          <Input className="w-full" type="date" value={dateFrom} max={dateTo || undefined} onChange={(event) => { setDateFrom(event.target.value); setPageNumber(1); }} />
        </label>
        <label className="block min-w-0 space-y-1 text-sm text-muted-foreground">
          <span className="block">Submission deadline to</span>
          <Input className="w-full" type="date" value={dateTo} min={dateFrom || undefined} onChange={(event) => { setDateTo(event.target.value); setPageNumber(1); }} />
        </label>
      </div>

      <div className="min-w-0 overflow-x-auto rounded-lg border border-border">
        <Table className="w-full min-w-[1280px] table-fixed">
          <TableHeader>
            <TableRow>
              <TableHead className="w-[5%]">Sr. No.</TableHead>
              <TableHead className="w-[8%]">Tender No.</TableHead>
              <TableHead className="w-[13%]">Title</TableHead>
              <TableHead className="w-[8%]">Department</TableHead>
              <TableHead className="w-[6%]">Category</TableHead>
              <TableHead className="w-[7%]">RESP Officer</TableHead>
              <TableHead className="w-[8%]">Assigned Role</TableHead>
              <TableHead className="w-[14%]">Submission Dates</TableHead>
              <TableHead className="w-[11%] text-right">Estimated Cost</TableHead>
              <TableHead className="w-[12%]">Status</TableHead>
              <TableHead className="w-[8%]">Created</TableHead>
              <TableHead className="w-[12%] text-right">Action</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading &&
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {                  Array.from({ length: 12 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))}

            {isError && (
              <TableRow>
                <TableCell colSpan={12} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-destructive">
                    <FileWarning className="h-8 w-8" />
                    <p className="text-sm">{error instanceof Error ? error.message : "Failed to load tenders."}</p>
                  </div>
                </TableCell>
              </TableRow>
            )}

            {!isLoading && !isError && data?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={12} className="py-12 text-center text-sm text-muted-foreground">
                  No tenders match your filters.
                </TableCell>
              </TableRow>
            )}

            {!isLoading &&
              !isError &&
              data?.items.map((tender, index) => (
                <TableRow key={tender.tenderId}>
                  <TableCell className="text-muted-foreground">
                    {(pageNumber - 1) * pageSize + index + 1}
                  </TableCell>
                  <TableCell className="break-words font-medium">
                    <Link href={`/tenders/${tender.tenderId}`} className="text-primary hover:underline">
                      {tender.tenderNumber}
                    </Link>
                  </TableCell>
                  <TableCell className="max-w-xs truncate">{tender.title}</TableCell>
                  <TableCell className="break-words">{tender.departmentName}</TableCell>
                  <TableCell className="break-words">{tender.tenderType}</TableCell>
                  <TableCell className="break-words">{tender.assignedRespOfficer || "Unassigned"}</TableCell>
                  <TableCell className="break-words">{tender.assignedRole || "No pending role"}</TableCell>
                  <TableCell className="whitespace-nowrap text-xs">
                    <div>Published {formatDate(tender.publicationDate)}</div>
                    <div>Opens {formatDate(tender.submissionStartDate)}</div>
                    <div>Closes {formatDate(tender.submissionDeadline)}</div>
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatCurrencyINR(tender.estimatedAmount)}
                  </TableCell>
                  <TableCell>
                    <div className="space-y-1">
                      <StatusBadge status={tender.currentStatus} />
                      {tender.currentStatus === "Tender Published" && (
                        <p className="text-xs text-muted-foreground">{tender.applicationStatus}</p>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-muted-foreground">{formatDate(tender.createdDate)}</TableCell>
                  <TableCell>
                    <div className="flex flex-col items-stretch gap-2">
                      <Button asChild variant="outline" size="sm">
                        <Link href={`/tenders/${tender.tenderId}`}><Eye className="mr-1 h-4 w-4" />View</Link>
                      </Button>
                      {tender.canApply && (
                        <Button asChild size="sm">
                          <Link href={`/tenders/${tender.tenderId}/apply`}>
                            <Send className="mr-1 h-4 w-4" />Apply
                          </Link>
                        </Button>
                      )}
                    </div>
                  </TableCell>
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
