"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";

import { useRequireRole } from "@/hooks/useRequireRole";
import { ROLE_NAMES } from "@/types/auth";
import { getInternalApplications } from "@/lib/api/applications";
import { formatDate } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

export default function InternalApplicationsPage() {
  const { isLoading: authLoading, isAuthorized } = useRequireRole([
    ROLE_NAMES.JuniorEngineer,
    ROLE_NAMES.TenderCell,
  ]);
  const [contractorId, setContractorId] = useState<string | null>(null);
  useEffect(() => {
    setContractorId(new URLSearchParams(window.location.search).get("contractorId"));
  }, []);
  const query = useQuery({
    queryKey: ["internal-contractor-applications"],
    queryFn: getInternalApplications,
    enabled: isAuthorized,
  });
  const applications = useMemo(() => query.data?.filter((application) =>
    !contractorId || application.contractorId === Number(contractorId)
  ) ?? [], [contractorId, query.data]);
  const tenderCounts = useMemo(() => {
    const counts = new Map<number, { tenderNumber: string; title: string; count: number }>();
    for (const application of applications) {
      const current = counts.get(application.tenderId);
      if (current) current.count += 1;
      else counts.set(application.tenderId, {
        tenderNumber: application.tenderNumber,
        title: application.tenderTitle,
        count: 1,
      });
    }
    return [...counts.values()];
  }, [applications]);

  if (authLoading || !isAuthorized) {
    return <main className="mx-auto max-w-7xl px-4 py-8"><Skeleton className="h-64 w-full" /></main>;
  }

  return (
    <main className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">Contractor Applications</h1>
          <p className="mt-1 text-sm text-muted-foreground">Applications and review history from the tender database.</p>
        </div>
        <Button asChild variant="outline"><Link href="/admin/contractors">Contractor List</Link></Button>
      </div>
      {contractorId && <p className="text-sm text-muted-foreground">Filtered by contractor record {contractorId}.</p>}
      {query.isError && <p role="alert" className="text-sm text-destructive">Could not load contractor applications.</p>}
      {query.isLoading ? <Skeleton className="h-48 w-full" /> : (
        <>
          <section aria-label="Application counts" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            {[
              ["Total Applications", applications.length],
              ["Pending", applications.filter((item) => item.status === "Submitted").length],
              ["Shortfall", applications.filter((item) => item.status === "Shortfall").length],
              ["Approved", applications.filter((item) => item.status === "Qualified").length],
              ["Rejected", applications.filter((item) => item.status === "Disqualified").length],
            ].map(([label, count]) => (
              <Card key={label}>
                <CardContent className="p-4">
                  <p className="text-sm text-muted-foreground">{label}</p>
                  <p className="mt-1 text-2xl font-semibold tabular-nums">{count}</p>
                </CardContent>
              </Card>
            ))}
          </section>

          <Card>
            <CardHeader><CardTitle>Tender-wise Applications</CardTitle></CardHeader>
            <CardContent className="space-y-2">
              {tenderCounts.length === 0 ? <p className="text-sm text-muted-foreground">No applications received.</p> :
                tenderCounts.map((tender) => (
                  <div key={tender.tenderNumber} className="flex justify-between gap-3 rounded-md bg-muted/40 p-3 text-sm">
                    <span>{tender.tenderNumber} · {tender.title}</span><strong>{tender.count}</strong>
                  </div>
                ))}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Applications</CardTitle></CardHeader>
            <CardContent className="p-0">
              {applications.length === 0 ? <p className="p-5 text-sm text-muted-foreground">No matching applications.</p> : (
                <div className="divide-y">
                  {applications.map((application) => (
                    <div key={application.technicalBidId} className="flex flex-wrap items-center justify-between gap-3 p-4">
                      <div className="min-w-0">
                        <p className="font-medium">{application.applicationReference} · {application.contractorName}</p>
                        <p className="mt-1 text-sm text-muted-foreground">{application.tenderNumber} · {application.tenderTitle}</p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {application.submittedAtUtc ? formatDate(application.submittedAtUtc) : "Draft"} · {application.documentCount} document(s)
                          {application.pendingMandatoryDocumentCount > 0 && ` · ${application.pendingMandatoryDocumentCount} required missing`}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        <StatusBadge status={application.status} />
                        <Button asChild size="sm"><Link href={`/applications/${application.technicalBidId}`}>View</Link></Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </main>
  );
}
