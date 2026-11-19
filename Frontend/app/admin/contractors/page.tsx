"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";

import { getContractors, getInternalApplications } from "@/lib/api/applications";
import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export default function ContractorListPage() {
  const contractorsQuery = useQuery({
    queryKey: ["admin-contractors"],
    queryFn: getContractors,
  });
  const applicationsQuery = useQuery({
    queryKey: ["internal-contractor-applications"],
    queryFn: getInternalApplications,
  });

  return (
    <div>
      <PageHeader title="Contractor List" description="Registered contractor profiles and their application history." />
      {(contractorsQuery.isError || applicationsQuery.isError) &&
        <p role="alert" className="mb-4 text-sm text-destructive">Could not load contractor records.</p>}
      {contractorsQuery.isLoading ? <Skeleton className="h-48 w-full" /> : (
        <div className="space-y-3">
          {contractorsQuery.data?.length === 0 && <p className="text-sm text-muted-foreground">No contractor profiles are registered.</p>}
          {contractorsQuery.data?.map((contractor) => {
            const applicationCount = applicationsQuery.data?.filter((application) =>
              application.contractorId === contractor.contractorId
            ).length ?? 0;
            return (
              <Card key={contractor.contractorId}>
                <CardContent className="flex flex-wrap items-center justify-between gap-4 p-4">
                  <div className="min-w-0">
                    <p className="font-medium">{contractor.name}</p>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {contractor.registrationNo} · {contractor.email ?? contractor.contactNo ?? "No contact on file"}
                    </p>
                    {contractor.address && <p className="mt-1 text-xs text-muted-foreground">{contractor.address}</p>}
                    <p className="mt-1 text-xs text-muted-foreground">{contractor.isActive ? "Active" : "Inactive"} · {applicationCount} application(s)</p>
                  </div>
                  <Button asChild size="sm" variant="outline">
                    <Link href={`/applications?contractorId=${contractor.contractorId}`}>View Applications</Link>
                  </Button>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
