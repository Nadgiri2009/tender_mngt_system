"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { useRequireRole } from "@/hooks/useRequireRole";
import { useAuth } from "@/contexts/AuthContext";
import { ROLE_NAMES } from "@/types/auth";
import {
  addApplicationRemark,
  downloadApplicationDocument,
  getApplicationDetails,
  reviewApplication,
  reviewApplicationDocument,
} from "@/lib/api/applications";
import { ApiError } from "@/lib/api/client";
import { formatCurrencyINR, formatDate } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

export default function InternalApplicationDetailPage() {
  const params = useParams<{ id: string }>() ?? { id: "" };
  const applicationId = Number(params.id);
  const { isLoading: authLoading, isAuthorized } = useRequireRole([
    ROLE_NAMES.JuniorEngineer,
    ROLE_NAMES.TenderCell,
  ]);
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [message, setMessage] = useState("");
  const [remarkType, setRemarkType] = useState<"Internal" | "Clarification">("Clarification");
  const [decisionRemarks, setDecisionRemarks] = useState("");
  const isTenderCell = user?.role === ROLE_NAMES.TenderCell;
  const query = useQuery({
    queryKey: ["internal-contractor-application", applicationId],
    queryFn: () => getApplicationDetails(applicationId),
    enabled: isAuthorized && Number.isInteger(applicationId) && applicationId > 0,
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ["internal-contractor-application", applicationId] });
    await queryClient.invalidateQueries({ queryKey: ["internal-contractor-applications"] });
    await queryClient.invalidateQueries({ queryKey: ["contractor-application-dashboard"] });
  };
  const remarkMutation = useMutation({
    mutationFn: () => addApplicationRemark(applicationId, message, remarkType),
    onSuccess: async () => {
      setMessage("");
      await refresh();
      toast.success(remarkType === "Clarification" ? "Clarification sent to contractor." : "Internal remark added.");
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });
  const decisionMutation = useMutation({
    mutationFn: (decision: "Qualified" | "Disqualified") =>
      reviewApplication(applicationId, decision, decisionRemarks),
    onSuccess: async (_, decision) => {
      setDecisionRemarks("");
      await refresh();
      toast.success(`Application marked ${decision.toLowerCase()}.`);
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });
  const documentMutation = useMutation({
    mutationFn: ({ documentId, status }: { documentId: number; status: "Verified" | "Shortfall" | "Rejected" }) =>
      reviewApplicationDocument(applicationId, documentId, status, message),
    onSuccess: async (_, variables) => {
      setMessage("");
      await refresh();
      toast.success(`Document marked ${variables.status.toLowerCase()}.`);
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });

  const download = async (documentId: number, fileName: string) => {
    try {
      const blob = await downloadApplicationDocument(applicationId, documentId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "The document could not be downloaded.");
    }
  };

  if (authLoading || !isAuthorized || query.isLoading) {
    return <main className="mx-auto max-w-5xl px-4 py-8"><Skeleton className="h-72 w-full" /></main>;
  }
  if (query.isError || !query.data) {
    return <main className="mx-auto max-w-5xl px-4 py-8"><p role="alert" className="text-sm text-destructive">Application not found or you are not assigned to review it.</p></main>;
  }

  const application = query.data;
  return (
    <main className="mx-auto max-w-5xl space-y-5 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/applications" className="text-sm text-muted-foreground hover:text-foreground">← Contractor Applications</Link>
          <h1 className="mt-2 text-2xl font-bold">{application.applicationReference}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{application.tenderNumber} · {application.tenderTitle}</p>
        </div>
        <StatusBadge status={application.status} />
      </div>

      <Card>
        <CardHeader><CardTitle>Contractor and Tender Details</CardTitle></CardHeader>
        <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
          <p><span className="text-muted-foreground">Company:</span> {application.contractorName}</p>
          <p><span className="text-muted-foreground">Representative:</span> {application.contractorRepresentativeName ?? "—"}</p>
          <p><span className="text-muted-foreground">Email:</span> {application.contractorEmail ?? "—"}</p>
          <p><span className="text-muted-foreground">Mobile:</span> {application.contractorMobile ?? "—"}</p>
          <p><span className="text-muted-foreground">Address:</span> {application.contractorAddress ?? "—"}</p>
          <p><span className="text-muted-foreground">Tender department:</span> {application.departmentName}</p>
          <p><span className="text-muted-foreground">Estimated amount:</span> {formatCurrencyINR(application.estimatedAmount)}</p>
          <p><span className="text-muted-foreground">Submitted:</span> {formatDate(application.submittedAtUtc ?? application.createdAtUtc)}</p>
          <p><span className="text-muted-foreground">Status:</span> {application.status}</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Uploaded Documents and Verification</CardTitle></CardHeader>
        <CardContent>
          {application.documents.length === 0 ? <p className="text-sm text-muted-foreground">No documents uploaded.</p> : (
            <div className="divide-y">
              {application.documents.map((item) => (
                <div key={item.documentId} className="flex flex-wrap items-start justify-between gap-3 py-3">
                  <div className="min-w-0">
                    <p className="text-sm font-medium">{item.documentType}: {item.fileName}</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Uploaded by {item.uploadedByName} · {formatDate(item.uploadedAtUtc)} · Verification: {item.verificationStatus ?? "Pending"}
                    </p>
                    {item.verificationRemarks && <p className="mt-1 whitespace-pre-wrap text-sm text-muted-foreground">{item.verificationRemarks}</p>}
                    {item.verifiedAtUtc && <p className="mt-1 text-xs text-muted-foreground">Reviewed {formatDate(item.verifiedAtUtc)}</p>}
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button size="sm" variant="outline" onClick={() => void download(item.documentId, item.fileName)}>Download</Button>
                    {isTenderCell && (
                      <>
                        <Button size="sm" variant="outline" disabled={documentMutation.isPending}
                          onClick={() => documentMutation.mutate({ documentId: item.documentId, status: "Verified" })}>Verify</Button>
                        <Button size="sm" variant="outline" disabled={documentMutation.isPending}
                          onClick={() => {
                            if (!message.trim()) {
                              toast.error("Enter a clarification remark before marking a shortfall.");
                              return;
                            }
                            documentMutation.mutate({ documentId: item.documentId, status: "Shortfall" });
                          }}>Shortfall</Button>
                      </>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Review and Remarks</CardTitle></CardHeader>
        <CardContent className="space-y-5">
          <div className="space-y-3">
            <Label htmlFor="application-remark">New remark</Label>
            <select id="application-remark" value={remarkType} onChange={(event) => setRemarkType(event.target.value as "Internal" | "Clarification")}
              className="block rounded-md border border-input bg-background px-3 py-2 text-sm">
              <option value="Clarification">Clarification for contractor</option>
              <option value="Internal">Internal remark</option>
            </select>
            <textarea value={message} onChange={(event) => setMessage(event.target.value)} maxLength={2000} rows={3}
              className="w-full rounded-md border border-input bg-background p-3 text-sm" />
            <Button disabled={remarkMutation.isPending || !message.trim()}
              onClick={() => remarkMutation.mutate()}>{remarkMutation.isPending ? "Saving..." : "Add Remark"}</Button>
          </div>
          <div className="space-y-3 border-t pt-4">
            <Label htmlFor="application-decision-remarks">Technical decision remarks</Label>
            <textarea id="application-decision-remarks" value={decisionRemarks} onChange={(event) => setDecisionRemarks(event.target.value)}
              maxLength={2000} rows={3} className="w-full rounded-md border border-input bg-background p-3 text-sm" />
            <div className="flex flex-wrap gap-2">
              <Button disabled={decisionMutation.isPending} onClick={() => decisionMutation.mutate("Qualified")}>Qualify Application</Button>
              <Button variant="destructive" disabled={decisionMutation.isPending || !decisionRemarks.trim()}
                onClick={() => decisionMutation.mutate("Disqualified")}>Disqualify Application</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Remark History</CardTitle></CardHeader>
        <CardContent>
          {application.remarks.length === 0 ? <p className="text-sm text-muted-foreground">No remarks yet.</p> : (
            <div className="space-y-3">
              {application.remarks.map((remark) => (
                <div key={remark.remarkId} className="rounded-md border p-3">
                  <p className="text-sm font-medium">{remark.remarkType} · {remark.authorName}{remark.authorRole ? ` (${remark.authorRole})` : ""}</p>
                  <p className="mt-1 whitespace-pre-wrap text-sm">{remark.message}</p>
                  <p className="mt-2 text-xs text-muted-foreground">{formatDate(remark.createdAtUtc)} · {remark.visibleToContractor ? "Visible to contractor" : "Internal"}</p>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Complete Application History</CardTitle></CardHeader>
        <CardContent>
          {application.history.length === 0 ? <p className="text-sm text-muted-foreground">No history available.</p> : (
            <ol className="space-y-3">
              {application.history.map((item) => (
                <li key={item.historyId} className="border-l-2 border-primary/30 pl-3">
                  <p className="text-sm font-medium">{item.eventType}{item.previousStatus ? `: ${item.previousStatus}` : ""}{item.newStatus ? ` → ${item.newStatus}` : ""}</p>
                  {item.remarks && <p className="mt-1 whitespace-pre-wrap text-sm text-muted-foreground">{item.remarks}</p>}
                  <p className="mt-1 text-xs text-muted-foreground">{item.authorName} · {formatDate(item.createdAtUtc)}</p>
                </li>
              ))}
            </ol>
          )}
        </CardContent>
      </Card>
    </main>
  );
}
