"use client";

import { ChangeEvent, FormEvent, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { useAuth } from "@/contexts/AuthContext";
import {
  addApplicationDocuments,
  addApplicationRemark,
  downloadApplicationDocument,
  getContractorApplication,
} from "@/lib/api/applications";
import { ApiError } from "@/lib/api/client";
import { formatDate } from "@/lib/utils";
import { ROLE_NAMES } from "@/types/auth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/tenders/StatusBadge";

export default function ContractorApplicationDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>() ?? { id: "" };
  const queryClient = useQueryClient();
  const { user, isLoading: authLoading } = useAuth();
  const applicationId = Number(params.id);
  const [selectedDocuments, setSelectedDocuments] = useState<Record<string, File>>({});
  const [response, setResponse] = useState("");

  useEffect(() => {
    if (!authLoading && !user) {
      router.replace(`/login?returnUrl=${encodeURIComponent(`/contractor/applications/${params.id}`)}`);
    }
  }, [authLoading, params.id, router, user]);

  const query = useQuery({
    queryKey: ["contractor-application", applicationId],
    queryFn: () => getContractorApplication(applicationId),
    enabled: Number.isInteger(applicationId) && applicationId > 0 && user?.role === ROLE_NAMES.ExternalBidder,
  });

  const refreshApplication = async () => {
    await queryClient.invalidateQueries({ queryKey: ["contractor-application", applicationId] });
    await queryClient.invalidateQueries({ queryKey: ["contractor-application-dashboard"] });
  };

  const uploadMutation = useMutation({
    mutationFn: () => addApplicationDocuments(
      applicationId,
      Object.entries(selectedDocuments).map(([documentType, file]) => ({ documentType, file }))
    ),
    onSuccess: async () => {
      setSelectedDocuments({});
      await refreshApplication();
      toast.success("Documents uploaded.");
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });

  const responseMutation = useMutation({
    mutationFn: () => addApplicationRemark(applicationId, response, "Response"),
    onSuccess: async () => {
      setResponse("");
      await refreshApplication();
      toast.success("Your response was sent.");
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });

  const uploadedTypes = useMemo(
    () => new Set(query.data?.documents.map((document) => document.documentType.toLowerCase()) ?? []),
    [query.data?.documents]
  );

  const download = async (documentId: number, fileName: string) => {
    try {
      const blob = await downloadApplicationDocument(applicationId, documentId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "The document could not be downloaded.");
    }
  };

  const handleFile = (type: string, maximumBytes: number, event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file && file.size > maximumBytes) {
      toast.error(`The selected file exceeds this document's ${Math.ceil(maximumBytes / 1_000_000)} MB limit.`);
      event.target.value = "";
      return;
    }
    setSelectedDocuments((current) => {
      const updated = { ...current };
      if (file) updated[type] = file;
      else delete updated[type];
      return updated;
    });
  };

  if (authLoading || !user || query.isLoading) {
    return <main className="mx-auto max-w-5xl px-4 py-8"><Skeleton className="h-72 w-full" /></main>;
  }
  if (user.role !== ROLE_NAMES.ExternalBidder) {
    return <main className="mx-auto max-w-5xl px-4 py-8"><p className="text-sm text-destructive">This page is for contractor accounts.</p></main>;
  }
  if (query.isError || !query.data) {
    return <main className="mx-auto max-w-5xl px-4 py-8"><p role="alert" className="text-sm text-destructive">Application not found or unavailable.</p></main>;
  }

  const application = query.data;
  const canRespond = application.status === "Shortfall";
  const canUpload = canRespond || application.status === "Draft";

  return (
    <main className="mx-auto max-w-5xl space-y-5 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/contractor/dashboard" className="text-sm text-muted-foreground hover:text-foreground">← Contractor dashboard</Link>
          <h1 className="mt-2 text-2xl font-bold">{application.tenderNumber} · {application.tenderTitle}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{application.applicationReference} · {formatDate(application.submittedAtUtc ?? application.createdAtUtc)}</p>
        </div>
        <StatusBadge status={application.status} />
      </div>

      <Card>
        <CardHeader><CardTitle>Application overview</CardTitle></CardHeader>
        <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
          <p><span className="text-muted-foreground">Contractor:</span> {application.contractorName}</p>
          <p><span className="text-muted-foreground">Status:</span> {application.status}</p>
          <p><span className="text-muted-foreground">Documents:</span> {application.documents.length}</p>
          <p><span className="text-muted-foreground">Required documents pending:</span> {application.pendingMandatoryDocumentCount}</p>
        </CardContent>
      </Card>

      {canUpload && (
        <Card>
          <CardHeader><CardTitle>{canRespond ? "Upload corrected documents" : "Complete draft documents"}</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            {application.requirements.map((requirement) => (
              <div key={requirement.documentType} className="space-y-2 rounded-md border p-3">
                <Label htmlFor={`application-document-${requirement.documentType}`}>{requirement.documentType}</Label>
                {requirement.description && <p className="text-sm text-muted-foreground">{requirement.description}</p>}
                {uploadedTypes.has(requirement.documentType.toLowerCase()) &&
                  <p className="text-xs text-muted-foreground">An earlier version is already on file. New uploads are retained as additional versions.</p>}
                <input
                  id={`application-document-${requirement.documentType}`}
                  type="file"
                  accept={requirement.allowedExtensions?.split(",").map((extension) => extension.trim().startsWith(".") ? extension.trim() : `.${extension.trim()}`).join(",") || ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png"}
                  onChange={(event) => handleFile(requirement.documentType, requirement.maxFileSizeBytes, event)}
                  className="block w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                />
                {selectedDocuments[requirement.documentType] &&
                  <p className="text-xs text-muted-foreground">Selected: {selectedDocuments[requirement.documentType]?.name}</p>}
              </div>
            ))}
            <Button type="button" disabled={uploadMutation.isPending || Object.keys(selectedDocuments).length === 0}
              onClick={() => uploadMutation.mutate()}>
              {uploadMutation.isPending ? "Uploading..." : "Upload Documents"}
            </Button>
          </CardContent>
        </Card>
      )}

      {canRespond && (
        <Card>
          <CardHeader><CardTitle>Respond to clarification</CardTitle></CardHeader>
          <CardContent>
            <form className="space-y-3" onSubmit={(event: FormEvent) => {
              event.preventDefault();
              if (!response.trim()) {
                toast.error("Enter a response before sending.");
                return;
              }
              responseMutation.mutate();
            }}>
              <Label htmlFor="application-response">Response</Label>
              <textarea id="application-response" value={response} onChange={(event) => setResponse(event.target.value)}
                maxLength={2000} rows={4} className="w-full rounded-md border border-input bg-background p-3 text-sm" />
              <Button type="submit" disabled={responseMutation.isPending}>
                {responseMutation.isPending ? "Sending..." : "Send Response"}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader><CardTitle>Uploaded documents</CardTitle></CardHeader>
        <CardContent>
          {application.documents.length === 0 ? <p className="text-sm text-muted-foreground">No documents uploaded.</p> : (
            <ul className="divide-y">
              {application.documents.map((item) => (
                <li key={item.documentId} className="flex flex-wrap items-center justify-between gap-2 py-3 text-sm">
                  <span>{item.documentType}: {item.fileName} <span className="text-xs text-muted-foreground">· {formatDate(item.uploadedAtUtc)}</span></span>
                  <Button size="sm" variant="outline" onClick={() => void download(item.documentId, item.fileName)}>Download</Button>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Remarks and clarifications</CardTitle></CardHeader>
        <CardContent>
          {application.remarks.length === 0 ? <p className="text-sm text-muted-foreground">No remarks yet.</p> : (
            <div className="space-y-3">
              {application.remarks.map((remark) => (
                <div key={remark.remarkId} className="rounded-md border p-3">
                  <p className="text-sm font-medium">{remark.remarkType} · {remark.authorName}{remark.authorRole ? ` (${remark.authorRole})` : ""}</p>
                  <p className="mt-1 whitespace-pre-wrap text-sm">{remark.message}</p>
                  <p className="mt-2 text-xs text-muted-foreground">{formatDate(remark.createdAtUtc)}</p>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Application history</CardTitle></CardHeader>
        <CardContent>
          {application.history.length === 0 ? <p className="text-sm text-muted-foreground">No history available.</p> : (
            <ol className="space-y-3">
              {application.history.map((item) => (
                <li key={item.historyId} className="border-l-2 border-primary/30 pl-3">
                  <p className="text-sm font-medium">{item.eventType}{item.newStatus ? ` · ${item.newStatus}` : ""}</p>
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
