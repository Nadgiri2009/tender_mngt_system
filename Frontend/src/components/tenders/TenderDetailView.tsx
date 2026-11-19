"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { notFound } from "next/navigation";
import { Pencil, ArrowLeft, Send } from "lucide-react";

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
import { ROLE_NAMES } from "@/types/auth";
import { ApplicationRequirementsEditor } from "@/components/tenders/ApplicationRequirementsEditor";

const apiOrigin = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5050/api").replace(/\/api\/?$/, "");

interface TenderDetailViewProps {
  tenderId: number;
}

export function TenderDetailView({ tenderId }: TenderDetailViewProps) {
  const { user } = useAuth();
  const isMainAdmin =
    user?.accountType === "Internal" && user.role === ROLE_NAMES.JuniorEngineer;

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
  const canEditDelete = isDraft && isMainAdmin;
  const applyHref = `/tenders/${tender.tenderId}/apply`;

  return (
    <main className="mx-auto max-w-5xl px-3 py-4 sm:px-4 sm:py-6 lg:px-8">
      <Link href="/tenders" className="mb-4 inline-flex items-center text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="mr-1 h-4 w-4" /> Back to Tenders
      </Link>

      <PageHeader
        title={tender.title}
        description={`${tender.tenderNumber} · ${tender.departmentName}`}
        actions={
          <>
            {tender.canApply && (
              <Button asChild size="sm">
                <Link href={applyHref}><Send className="mr-1 h-4 w-4" />Apply for Tender</Link>
              </Button>
            )}
            {canEditDelete && (
              <>
              <Button asChild variant="outline" size="sm">
                <Link href={`/tenders/${tender.tenderId}/edit`}>
                  <Pencil className="mr-1 h-4 w-4" /> Edit
                </Link>
              </Button>
              <DeleteTenderDialog tenderId={tender.tenderId} tenderNumber={tender.tenderNumber} />
              </>
            )}
          </>
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
              {tender.eligibilityCriteria && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Eligibility Criteria</p>
                  <p className="mt-1 whitespace-pre-wrap text-sm text-foreground break-words">{tender.eligibilityCriteria}</p>
                </div>
              )}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-xs text-muted-foreground">Tender Method</p>
                  <p className="text-sm font-medium break-words">{tender.tenderType || "—"}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Tender Category</p>
                  <p className="text-sm font-medium break-words">{tender.tenderCategory ?? "—"}</p>
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
                  <p className="text-xs text-muted-foreground">Submission Opens</p>
                  <p className="text-sm font-medium break-words">{formatDate(tender.submissionStartDate)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Publication Date</p>
                  <p className="text-sm font-medium break-words">{formatDate(tender.publicationDate)}</p>
                </div>
                {tender.preBidMeetingUtc && (
                  <div>
                    <p className="text-xs text-muted-foreground">Pre-bid Meeting</p>
                    <p className="text-sm font-medium break-words">{formatDate(tender.preBidMeetingUtc)}</p>
                  </div>
                )}
                {tender.technicalBidOpeningScheduledUtc && (
                  <div>
                    <p className="text-xs text-muted-foreground">Technical Bid Opening</p>
                    <p className="text-sm font-medium break-words">{formatDate(tender.technicalBidOpeningScheduledUtc)}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Application Status</p>
                  <p className="text-sm font-medium break-words">{tender.applicationStatus}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Created By</p>
                  <p className="text-sm font-medium break-words">{tender.createdByName || "—"}</p>
                </div>
                {tender.preBidDetails && (
                  <div className="border-t pt-4">
                    <p className="text-sm font-medium text-muted-foreground">Pre-bid Details</p>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-foreground break-words">{tender.preBidDetails}</p>
                    {tender.preBidLocation && <p className="mt-1 text-sm text-muted-foreground">Location: {tender.preBidLocation}</p>}
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Created Date</p>
                  <p className="text-sm font-medium break-words">{formatDate(tender.createdDate)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {canEditDelete && <ApplicationRequirementsEditor tenderId={tender.tenderId} />}

          <Card>
            <CardContent className="pt-6">
              <BOQItemsPanel tenderId={tender.tenderId} items={tender.boqItems} editable={isDraft} />
            </CardContent>
          </Card>

          {tender.boqItems.length > 0 && (
            <Card>
              <CardHeader><CardTitle>Scope and technical requirements</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                {tender.boqItems.map((item) => (
                  <div key={item.boqId} className="rounded-md border p-3">
                    <p className="text-sm font-medium">{item.itemName}</p>
                    {item.description && <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>}
                    <p className="mt-1 text-xs text-muted-foreground">{item.quantity} {item.unit}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
          )}

          {tender.currentStatus === "Tender Published" && (
            <Card>
              <CardHeader><CardTitle>Terms & Conditions</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm text-muted-foreground">
                  Review the portal terms and the tender-specific conditions and addenda before applying.
                </p>
                <Button asChild variant="outline" size="sm">
                  <Link href="/terms">Read Terms & Conditions</Link>
                </Button>
              </CardContent>
            </Card>
          )}

          {user?.role !== ROLE_NAMES.ExternalBidder && (
            <BidsPanel tenderId={tender.tenderId} currentStatus={tender.currentStatus} />
          )}

          {tender.documents.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Attachments</CardTitle>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {tender.documents.map((doc) => (
                    <li key={doc.documentId} className="text-sm">
                      <a className="font-medium text-primary underline" href={`${apiOrigin}/${doc.filePath.replace(/^[\\/]+/, "")}`} target="_blank" rel="noreferrer">
                        {doc.fileName}
                      </a>{" "}
                      <span className="text-muted-foreground">({doc.documentType})</span>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}

          {user && user.role !== ROLE_NAMES.ExternalBidder && (
            <div className="w-full">
              <WorkflowPanel tenderId={tender.tenderId} currentStatus={tender.currentStatus} departmentName={tender.departmentName} />
            </div>
          )}
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
