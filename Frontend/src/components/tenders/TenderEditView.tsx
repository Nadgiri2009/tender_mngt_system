"use client";

import { useQuery } from "@tanstack/react-query";
import { notFound } from "next/navigation";

import { fetchTenderById } from "@/lib/api/tenders";
import { ApiError } from "@/lib/api/client";

import { PageHeader } from "@/components/layout/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { EditTenderForm } from "@/components/tenders/EditTenderForm";

export function TenderEditView({ tenderId }: { tenderId: number }) {
  const { data: tender, isLoading, isError, error } = useQuery({
    queryKey: ["tender", tenderId],
    queryFn: () => fetchTenderById(tenderId),
    retry: (failureCount, err) => {
      if (err instanceof ApiError && err.statusCode === 404) return false;
      return failureCount < 1;
    },
  });

  if (isLoading) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="mt-4 h-96 w-full" />
      </main>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.statusCode === 404) notFound();
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-sm text-destructive">
          {error instanceof Error ? error.message : "Failed to load tender."}
        </p>
      </main>
    );
  }

  if (!tender) return null;

  return (
    <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <PageHeader title="Edit Tender" description={tender.tenderNumber} />
      <EditTenderForm tender={tender} />
    </main>
  );
}
