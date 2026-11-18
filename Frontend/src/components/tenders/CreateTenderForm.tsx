"use client";

import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useState } from "react";

import { createTenderSchema, type CreateTenderFormValues } from "@/lib/validations/tender";
import { createTender } from "@/lib/api/tenders";
import { fetchTenderCategories, fetchTenderTypes } from "@/lib/api/lookups";
import { ApiError } from "@/lib/api/client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DepartmentSelect } from "@/components/tenders/DepartmentSelect";
import { useAuth } from "@/contexts/AuthContext";
import { ROLE_NAMES } from "@/types/auth";

const allowedExtensions = new Set([".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png"]);
const maxFileSizeBytes = 50_000_000;

function toUtcIso(value?: string) {
  return value ? new Date(value).toISOString() : undefined;
}

export function CreateTenderForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const isMainAdmin = user?.role === ROLE_NAMES.JuniorEngineer;
  const [files, setFiles] = useState<File[]>([]);
  const [preBidDocument, setPreBidDocument] = useState<File>();
  const [preBidError, setPreBidError] = useState<string>();
  const [attachmentError, setAttachmentError] = useState<string>();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<CreateTenderFormValues>({
    resolver: zodResolver(createTenderSchema),
    defaultValues: {
      title: "",
      description: "",
      departmentId: user?.departmentId ?? undefined,
      tenderType: "",
      tenderCategory: "",
      submissionStartDate: "",
      submissionDeadline: "",
    },
  });

  const mutation = useMutation({
    mutationFn: (values: CreateTenderFormValues) =>
      createTender(
        {
          title: values.title,
          description: values.description || undefined,
          departmentId: values.departmentId,
          estimatedAmount: values.estimatedAmount,
          tenderType: values.tenderType,
          tenderCategory: values.tenderCategory,
          budgetInformation: values.budgetInformation || undefined,
          technicalSanctionNumber: values.technicalSanctionNumber || undefined,
          technicalSanctionAuthority: values.technicalSanctionAuthority || undefined,
          technicalSanctionDate: values.technicalSanctionDate || undefined,
          preBidMeetingUtc: toUtcIso(values.preBidMeetingUtc),
          preBidLocation: values.preBidLocation || undefined,
          preBidDetails: values.preBidDetails || undefined,
          technicalBidOpeningScheduledUtc: toUtcIso(values.technicalBidOpeningScheduledUtc),
          submissionStartDate: toUtcIso(values.submissionStartDate),
          submissionDeadline: toUtcIso(values.submissionDeadline),
        },
        files,
        preBidDocument
      ),
    onSuccess: (tender) => {
      toast.success(`Tender ${tender.tenderNumber} created as Draft.`);
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      router.push("/tenders");
    },
    onError: (error: ApiError) => {
      toast.error(error.message || "Failed to create tender.");
    },
  });

  const departmentValue = watch("departmentId");
  const tenderTypeValue = watch("tenderType");
  const tenderCategoryValue = watch("tenderCategory");

  const { data: tenderTypes } = useQuery({
    queryKey: ["tender-types"],
    queryFn: fetchTenderTypes,
  });
  const { data: tenderCategories } = useQuery({
    queryKey: ["tender-categories"],
    queryFn: fetchTenderCategories,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>New Tender Requirement</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => {
            if (values.preBidMeetingUtc && !preBidDocument) {
              setPreBidError("Upload the pre-bid meeting suggestion when a meeting is scheduled.");
              return;
            }
            if (attachmentError) return;
            mutation.mutate(values);
          })}
          className="grid grid-cols-1 gap-6 sm:grid-cols-2"
        >
          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="title">Title</Label>
            <Input id="title" placeholder="e.g. Road Resurfacing - Ward 12" {...register("title")} />
            {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="description">Description and Scope of Work</Label>
            <Textarea id="description" rows={4} placeholder="Scope of work..." {...register("description")} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="departmentId">Department</Label>
            <DepartmentSelect
              value={departmentValue ? String(departmentValue) : ""}
              onChange={(v) => setValue("departmentId", Number(v), { shouldValidate: true })}
            />
            {isMainAdmin && <p className="text-xs text-muted-foreground">As Main Admin, you can create tenders for any active department.</p>}
            {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="tenderType">Tender Method</Label>
            <Select
              value={tenderTypeValue}
              onValueChange={(v) => setValue("tenderType", v, { shouldValidate: true })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select tender type" />
              </SelectTrigger>
              <SelectContent>
                {tenderTypes?.map((t) => (
                  <SelectItem key={t} value={t}>
                    {t}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.tenderType && <p className="text-sm text-destructive">{errors.tenderType.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="tenderCategory">Tender Category</Label>
            <Select
              value={tenderCategoryValue}
              onValueChange={(v) => setValue("tenderCategory", v, { shouldValidate: true })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select tender category" />
              </SelectTrigger>
              <SelectContent>
                {tenderCategories?.map((category) => (
                  <SelectItem key={category} value={category}>
                    {category}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.tenderCategory && <p className="text-sm text-destructive">{errors.tenderCategory.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="estimatedAmount">Estimated Amount (₹)</Label>
            <Input
              id="estimatedAmount"
              type="number"
              step="0.01"
              min="0.01"
              placeholder="0.00"
              {...register("estimatedAmount")}
            />
            {errors.estimatedAmount && (
              <p className="text-sm text-destructive">{errors.estimatedAmount.message}</p>
            )}
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="budgetInformation">Budget Details</Label>
            <Textarea id="budgetInformation" rows={3} {...register("budgetInformation")} />
            {errors.budgetInformation && <p className="text-sm text-destructive">{errors.budgetInformation.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="technicalSanctionNumber">Technical Sanction Number</Label>
            <Input id="technicalSanctionNumber" {...register("technicalSanctionNumber")} />
            {errors.technicalSanctionNumber && <p className="text-sm text-destructive">{errors.technicalSanctionNumber.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="technicalSanctionAuthority">Technical Sanction Authority</Label>
            <Input id="technicalSanctionAuthority" {...register("technicalSanctionAuthority")} />
            {errors.technicalSanctionAuthority && <p className="text-sm text-destructive">{errors.technicalSanctionAuthority.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="technicalSanctionDate">Technical Sanction Date</Label>
            <Input id="technicalSanctionDate" type="date" {...register("technicalSanctionDate")} />
            {errors.technicalSanctionDate && <p className="text-sm text-destructive">{errors.technicalSanctionDate.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="submissionStartDate">Application Start Date</Label>
            <Input id="submissionStartDate" type="datetime-local" {...register("submissionStartDate")} />
            {errors.submissionStartDate && (
              <p className="text-sm text-destructive">{errors.submissionStartDate.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="submissionDeadline">Submission Deadline</Label>
            <Input id="submissionDeadline" type="datetime-local" {...register("submissionDeadline")} />
            {errors.submissionDeadline && (
              <p className="text-sm text-destructive">{errors.submissionDeadline.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="technicalBidOpeningScheduledUtc">Technical Bid Opening</Label>
            <Input
              id="technicalBidOpeningScheduledUtc"
              type="datetime-local"
              {...register("technicalBidOpeningScheduledUtc")}
            />
            {errors.technicalBidOpeningScheduledUtc && (
              <p className="text-sm text-destructive">{errors.technicalBidOpeningScheduledUtc.message}</p>
            )}
          </div>

          <div className="sm:col-span-2 space-y-4 rounded-md border p-4">
            <p className="text-sm font-medium">Pre-bid Meeting (optional)</p>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="preBidMeetingUtc">Meeting Date and Time</Label>
                <Input id="preBidMeetingUtc" type="datetime-local" {...register("preBidMeetingUtc")} />
                {errors.preBidMeetingUtc && <p className="text-sm text-destructive">{errors.preBidMeetingUtc.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="preBidLocation">Meeting Location</Label>
                <Input id="preBidLocation" {...register("preBidLocation")} />
                {errors.preBidLocation && <p className="text-sm text-destructive">{errors.preBidLocation.message}</p>}
              </div>
              <div className="sm:col-span-2 space-y-2">
                <Label htmlFor="preBidDetails">Meeting Details</Label>
                <Textarea id="preBidDetails" rows={3} {...register("preBidDetails")} />
                {errors.preBidDetails && <p className="text-sm text-destructive">{errors.preBidDetails.message}</p>}
              </div>
              <div className="sm:col-span-2 space-y-2">
                <Label htmlFor="preBidDocument">Pre-bid Meeting Suggestion / Upload</Label>
                <Input
                  id="preBidDocument"
                  type="file"
                  accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png"
                  onChange={(event) => {
                    const file = event.target.files?.[0];
                    if (file && (file.size <= 0 || file.size > maxFileSizeBytes ||
                      !allowedExtensions.has(file.name.slice(file.name.lastIndexOf(".")).toLowerCase()))) {
                      setPreBidDocument(undefined);
                      setPreBidError("Choose a supported document type no larger than 50 MB.");
                      return;
                    }
                    setPreBidDocument(file);
                    setPreBidError(undefined);
                  }}
                />
                <p className="text-xs text-muted-foreground">
                  Required when a pre-bid meeting is scheduled. Maximum file size: 50 MB.
                </p>
                {preBidError && <p className="text-sm text-destructive">{preBidError}</p>}
              </div>
            </div>
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="files">Attachments</Label>
            <Input
              id="files"
              type="file"
              multiple
              accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png"
              onChange={(event) => {
                const selectedFiles = event.target.files ? Array.from(event.target.files) : [];
                const invalidFile = selectedFiles.find((file) =>
                  file.size <= 0 || file.size > maxFileSizeBytes ||
                  !allowedExtensions.has(file.name.slice(file.name.lastIndexOf(".")).toLowerCase())
                );
                if (invalidFile) {
                  setFiles([]);
                  setAttachmentError(`${invalidFile.name} must be a supported document type no larger than 50 MB.`);
                  return;
                }
                setFiles(selectedFiles);
                setAttachmentError(undefined);
              }}
            />
            <p className="text-xs text-muted-foreground">
              Optional. Attach requirement notes, drawings, or supporting documents.
            </p>
            {attachmentError && <p className="text-sm text-destructive">{attachmentError}</p>}
          </div>

          <div className="sm:col-span-2 flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={() => router.push("/tenders")} className="w-full sm:w-auto">
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || mutation.isPending} className="w-full sm:w-auto">
              {mutation.isPending ? "Creating..." : "Create Tender"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
