"use client";

import { ChangeEvent, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { ArrowLeft, FileText } from "lucide-react";

import { fetchTenderById } from "@/lib/api/tenders";
import {
  getApplicationRequirements,
  saveApplicationDraft,
  submitApplication,
  type ContractorApplicationDetail,
} from "@/lib/api/applications";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { ROLE_NAMES } from "@/types/auth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";

interface TenderApplicationPageProps {
  params: Promise<{ id: string }>;
}

export default function TenderApplicationPage({ params }: TenderApplicationPageProps) {
  const router = useRouter();
  const { user, isLoading: authLoading } = useAuth();
  const [tenderId, setTenderId] = useState<number | null>(null);
  const [selectedDocuments, setSelectedDocuments] = useState<Record<string, File>>({});
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [fileError, setFileError] = useState<string | null>(null);
  const [submission, setSubmission] = useState<ContractorApplicationDetail | null>(null);

  useEffect(() => {
    let active = true;
    void params.then(({ id }) => {
      const parsedId = Number(id);
      if (active) setTenderId(Number.isInteger(parsedId) && parsedId > 0 ? parsedId : 0);
    });
    return () => {
      active = false;
    };
  }, [params]);

  const returnUrl = tenderId ? `/tenders/${tenderId}/apply` : null;

  useEffect(() => {
    if (authLoading || !returnUrl) return;
    if (!user) {
      router.replace(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
  }, [authLoading, returnUrl, router, user]);

  const { data: tender, isLoading: tenderLoading, isError: tenderError } = useQuery({
    queryKey: ["tender", tenderId],
    queryFn: () => fetchTenderById(tenderId!),
    enabled: tenderId !== null && !!user,
    retry: false,
  });

  const requirementsQuery = useQuery({
    queryKey: ["tender-application-requirements", tenderId],
    queryFn: () => getApplicationRequirements(tenderId!),
    enabled: tenderId !== null && tenderId > 0 && !!user,
    retry: false,
  });

  const saveDraftMutation = useMutation({
    mutationFn: () => saveApplicationDraft(
      tenderId!,
      Object.entries(selectedDocuments).map(([documentType, file]) => ({ documentType, file }))
    ),
    onSuccess: (result) => {
      toast.success("Your draft has been saved.");
      setSelectedDocuments({});
      void requirementsQuery.refetch();
    },
    onError: (error: Error) => {
      toast.error(error instanceof ApiError ? error.message : error.message || "Draft could not be saved.");
    },
  });

  const mutation = useMutation({
    mutationFn: async () => {
      const draft = await saveApplicationDraft(
        tenderId!,
        Object.entries(selectedDocuments).map(([documentType, file]) => ({ documentType, file }))
      );
      return submitApplication(draft.technicalBidId);
    },
    onSuccess: (result) => {
      setSubmission(result);
      toast.success("Your tender application was submitted.");
    },
    onError: (error: Error) => {
      toast.error(error instanceof ApiError ? error.message : error.message || "Application submission failed.");
    },
  });

  const handleFile = (
    documentType: string,
    maxFileSizeBytes: number,
    event: ChangeEvent<HTMLInputElement>
  ) => {
    const selected = event.target.files?.[0];
    setFileError(null);
    if (selected && selected.size > maxFileSizeBytes) {
      setFileError(`The selected file exceeds this document's ${Math.round(maxFileSizeBytes / 1_000_000)} MB limit.`);
      event.target.value = "";
      return;
    }
    setSelectedDocuments((current) => {
      const updated = { ...current };
      if (selected) updated[documentType] = selected;
      else delete updated[documentType];
      return updated;
    });
  };

  if (authLoading || tenderId === null || !user) {
    return <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8"><Skeleton className="h-56 w-full" /></main>;
  }

  if (tenderId === 0) {
    return <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8"><p className="text-sm text-destructive">Invalid tender reference.</p><Button asChild variant="outline" className="mt-4"><Link href="/tenders">Browse tenders</Link></Button></main>;
  }

  if (user.role !== ROLE_NAMES.ExternalBidder) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <Card>
          <CardHeader><CardTitle>Contractor account required</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">Only a registered Contractor/Vendor account can apply for a tender.</p>
            <Button asChild variant="outline"><Link href={`/tenders/${tenderId}`}>Return to tender</Link></Button>
          </CardContent>
        </Card>
      </main>
    );
  }

  if (tenderLoading) {
    return <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8"><Skeleton className="h-56 w-full" /></main>;
  }

  if (tenderError || !tender) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-sm text-destructive">Could not load this tender. It may no longer be publicly available.</p>
        <Button asChild variant="outline" className="mt-4"><Link href="/tenders">Browse tenders</Link></Button>
      </main>
    );
  }

  if (submission) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <Card>
          <CardHeader>
            <CardTitle>Application submitted</CardTitle>
            <CardDescription>Your submission has been recorded for {tender.tenderNumber}.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm">Application reference: <strong className="font-mono">{submission.applicationReference}</strong></p>
            <p className="text-sm text-muted-foreground">Keep this reference for your records.</p>
            <div className="flex flex-wrap gap-2">
              <Button asChild><Link href={`/contractor/applications/${submission.technicalBidId}`}>View application</Link></Button>
              <Button asChild variant="outline"><Link href="/contractor/dashboard">Contractor dashboard</Link></Button>
            </div>
          </CardContent>
        </Card>
      </main>
    );
  }

  if (!tender.canApply) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <Card>
          <CardHeader><CardTitle>Applications are not available</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">{tender.applicationStatus || "This tender is not accepting applications."}</p>
            <Button asChild variant="outline"><Link href={`/tenders/${tenderId}`}>Return to tender details</Link></Button>
          </CardContent>
        </Card>
      </main>
    );
  }

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
      <Link href={`/tenders/${tenderId}`} className="mb-4 inline-flex items-center text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="mr-1 h-4 w-4" /> Back to tender
      </Link>
      <Card>
        <CardHeader>
          <CardTitle>Apply for Tender</CardTitle>
          <CardDescription>{tender.tenderNumber} · {tender.title}</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            className="space-y-6"
            onSubmit={(event) => {
              event.preventDefault();
              if (!tender.canApply) {
                toast.error("This tender is not currently accepting applications.");
                return;
              }
              if (!termsAccepted) {
                toast.error("Accept the Terms & Conditions before submitting.");
                return;
              }
              mutation.mutate();
            }}
          >
            <div className="rounded-md border bg-muted/30 p-4">
              <p className="text-sm font-medium">Review the official tender notice and attachments before applying.</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Upload documents according to the tender-specific requirements below.
              </p>
              {tender.eligibilityCriteria && (
                <p className="mt-3 whitespace-pre-wrap text-sm text-muted-foreground">
                  <strong>Eligibility:</strong> {tender.eligibilityCriteria}
                </p>
              )}
              {tender.documents.length > 0 && (
                <ul className="mt-3 space-y-2">
                  {tender.documents.map((document) => (
                    <li key={document.documentId} className="flex items-center gap-2 text-sm">
                      <FileText className="h-4 w-4 shrink-0" />
                      <a className="text-primary underline" href={`${(process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5050/api").replace(/\/api\/?$/, "")}/${document.filePath.replace(/^[\\/]+/, "")}`} target="_blank" rel="noreferrer">
                        {document.fileName}
                      </a>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="space-y-4">
              <h2 className="text-sm font-semibold">Tender-specific required documents</h2>
              {requirementsQuery.isLoading && <Skeleton className="h-16 w-full" />}
              {requirementsQuery.isError && <p role="alert" className="text-sm text-destructive">Could not load this tender's document requirements.</p>}
              {!requirementsQuery.isLoading && !requirementsQuery.isError && requirementsQuery.data?.length === 0 && (
                <p className="text-sm text-muted-foreground">No additional document requirements are configured. Follow the published tender notice.</p>
              )}
              {requirementsQuery.data?.map((requirement) => (
                <div key={requirement.documentType} className="space-y-2 rounded-md border p-3">
                  <Label htmlFor={`application-document-${requirement.documentType}`}>
                    {requirement.documentType}
                    {requirement.isMandatory ? <span className="ml-1 text-destructive">*</span> : <span className="ml-1 text-muted-foreground">(optional)</span>}
                  </Label>
                  {requirement.description && <p className="text-sm text-muted-foreground">{requirement.description}</p>}
                  <input
                    id={`application-document-${requirement.documentType}`}
                    type="file"
                    accept={requirement.allowedExtensions?.split(",").map((extension) => extension.trim().startsWith(".") ? extension.trim() : `.${extension.trim()}`).join(",") || ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png"}
                    onChange={(event) => handleFile(requirement.documentType, requirement.maxFileSizeBytes, event)}
                    className="block w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  />
                  {selectedDocuments[requirement.documentType] && (
                    <p className="text-xs text-muted-foreground">Selected: {selectedDocuments[requirement.documentType]?.name}</p>
                  )}
                  <p className="text-xs text-muted-foreground">Maximum {Math.ceil(requirement.maxFileSizeBytes / 1_000_000)} MB.</p>
                </div>
              ))}
              {fileError && <p role="alert" className="text-sm text-destructive">{fileError}</p>}
            </div>

            <div className="flex items-start gap-2">
              <input
                id="application-terms"
                type="checkbox"
                checked={termsAccepted}
                onChange={(event) => setTermsAccepted(event.target.checked)}
                className="mt-1 h-4 w-4 shrink-0 accent-primary"
              />
              <Label htmlFor="application-terms" className="font-normal leading-5">
                I have reviewed and accept the <Link href="/terms" target="_blank" className="text-primary underline">portal Terms &amp; Conditions</Link> and the selected tender's official conditions.
              </Label>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" disabled={saveDraftMutation.isPending || !tender.canApply || requirementsQuery.isLoading || requirementsQuery.isError}
                onClick={() => saveDraftMutation.mutate()}>
                {saveDraftMutation.isPending ? "Saving draft..." : "Save as Draft"}
              </Button>
              <Button type="submit" disabled={mutation.isPending || !tender.canApply || requirementsQuery.isLoading || requirementsQuery.isError}>
                {mutation.isPending ? "Submitting application..." : "Submit Application"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </main>
  );
}
