"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight } from "lucide-react";

import { fetchPublicTenders } from "@/lib/api/tenders";
import { formatCurrencyINR, formatDate } from "@/lib/utils";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

export function LatestTendersSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["tenders", "latest-published"],
    queryFn: () =>
      fetchPublicTenders({
        pageNumber: 1,
        pageSize: 5,
        sortBy: "createdDate",
        sortDescending: true,
      }),
  });

  return (
    <section className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-foreground">Latest Tender Notices</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Recently published tender notices.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href="/tenders">
            View All <ArrowRight className="ml-1 h-4 w-4" />
          </Link>
        </Button>
      </div>

      <Card className="mt-6">
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>S. No.</TableHead>
                <TableHead>Tender No.</TableHead>
                <TableHead>Title</TableHead>
                <TableHead>Department</TableHead>
                <TableHead className="text-right">Estimated Cost</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Published</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading &&
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-4 w-full" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}

              {isError && (
                <TableRow>
                  <TableCell colSpan={7} className="py-8 text-center text-sm text-destructive">
                    Could not load tenders right now.
                  </TableCell>
                </TableRow>
              )}

              {!isLoading && !isError && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="py-8 text-center text-sm text-muted-foreground">
                    No tenders published yet.
                  </TableCell>
                </TableRow>
              )}

              {!isLoading &&
                !isError &&
                data?.items.map((tender, index) => (
                  <TableRow key={tender.tenderId}>
                    <TableCell className="text-muted-foreground">{index + 1}</TableCell>
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
                    <TableCell className="text-muted-foreground">{formatDate(tender.publicationDate)}</TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </section>
  );
}
