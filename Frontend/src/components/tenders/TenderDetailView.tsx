"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { notFound } from "next/navigation";
import { Pencil, ArrowLeft } from "lucide-react";

import { fetchTenderById } from "@/lib/api/tenders";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyINR, formatDate } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";

import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";
import { BOQItemsPanel } from "@/components/tenders/BOQItemsPanel";
import { WorkflowPanel } from "@/components/tenders/WorkflowPanel";
import { BidsPanel } from "@/components/tenders/BidsPanel";
import { DeleteTenderDialog } from "@/components/tenders/DeleteTenderDialog";

interface TenderDetailViewProps {
  tenderId: number;
}

export function TenderDetailView({ tenderId }: TenderDetailViewProps) {
  const { user } = useAuth();
  const isAdmin = user?.role === "Admin";

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
      <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="mt-4 h-48 w-full" />
      </main>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.statusCode === 404) {
      notFound();
    }
    return (
      <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-sm text-destructive">
          {error instanceof Error ? error.message : "Failed to load tender."}
        </p>
      </main>
    );
  }

  if (!tender) return null;

  const isDraft = tender.currentStatus === "Draft";
  const canEditDelete = isDraft && isAdmin;

  return (
    <main className="mx-auto max-w-5xl px-3 py-4 sm:px-4 sm:py-6 lg:px-8">
      <Link href="/tenders" className="mb-4 inline-flex items-center text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="mr-1 h-4 w-4" /> Back to Tenders
      </Link>

      <PageHeader
        title={tender.title}
        description={`${tender.tenderNumber} · ${tender.departmentName}`}
        actions={
          canEditDelete ? (
            <>
              <Button asChild variant="outline" size="sm">
                <Link href={`/tenders/${tender.tenderId}/edit`}>
                  <Pencil className="mr-1 h-4 w-4" /> Edit
                </Link>
              </Button>
              <DeleteTenderDialog tenderId={tender.tenderId} tenderNumber={tender.tenderNumber} />
            </>
          ) : undefined
        }
      />

      <div className="grid grid-cols-1 gap-4 sm:gap-5 lg:grid-cols-3 lg:gap-6">
        <div className="space-y-4 sm:space-y-5 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Description</p>
                <p className="mt-1 text-sm text-foreground break-words">{tender.description || "No description provided."}</p>
              </div>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-xs text-muted-foreground">Tender Type</p>
                  <p className="text-sm font-medium break-words">{tender.tenderType}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Estimated Cost</p>
                  <p className="text-sm font-medium break-words">{formatCurrencyINR(tender.estimatedAmount)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Submission Deadline</p>
                  <p className="text-sm font-medium break-words">{formatDate(tender.submissionDeadline)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Created By</p>
                  <p className="text-sm font-medium break-words">{tender.createdByName || "—"}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Created Date</p>
                  <p className="text-sm font-medium break-words">{formatDate(tender.createdDate)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <BOQItemsPanel tenderId={tender.tenderId} items={tender.boqItems} editable={isDraft} />
            </CardContent>
          </Card>

          <BidsPanel tenderId={tender.tenderId} currentStatus={tender.currentStatus} />

          {tender.documents.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Attachments</CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {tender.documents.map((doc) => (
                    <li key={doc.documentId} className="text-sm">
                      <span className="font-medium">{doc.fileName}</span>{" "}
                      <span className="text-muted-foreground">({doc.documentType})</span>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}

          <div className="w-full">
            <WorkflowPanel tenderId={tender.tenderId} currentStatus={tender.currentStatus} departmentName={tender.departmentName} />
          </div>
        </div>

        <div className="space-y-4 sm:space-y-5">
          <Card>
            <CardHeader>
              <CardTitle>Status</CardTitle>
            </CardHeader>
            <CardContent>
              <StatusBadge status={tender.currentStatus} />
            </CardContent>
          </Card>
        </div>
      </div>
    </main>
  );
}
