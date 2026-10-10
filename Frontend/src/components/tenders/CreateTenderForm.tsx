"use client";

import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useState } from "react";

import { createTenderSchema, type CreateTenderFormValues } from "@/lib/validations/tender";
import { createTender } from "@/lib/api/tenders";
import { fetchTenderTypes } from "@/lib/api/lookups";
import { ApiError } from "@/lib/api/client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DepartmentSelect } from "@/components/tenders/DepartmentSelect";

export function CreateTenderForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [files, setFiles] = useState<File[]>([]);

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
      tenderType: "",
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
          submissionDeadline: values.submissionDeadline || undefined,
        },
        files
      ),
    onSuccess: (tender) => {
      toast.success(`Tender ${tender.tenderNumber} created as Draft.`);
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      router.push(`/tenders/${tender.tenderId}`);
    },
    onError: (error: ApiError) => {
      toast.error(error.message || "Failed to create tender.");
    },
  });

  const departmentValue = watch("departmentId");
  const tenderTypeValue = watch("tenderType");

  const { data: tenderTypes } = useQuery({
    queryKey: ["tender-types"],
    queryFn: fetchTenderTypes,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>New Tender Requirement</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => mutation.mutate(values))}
          className="grid grid-cols-1 gap-6 sm:grid-cols-2"
        >
          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="title">Title</Label>
            <Input id="title" placeholder="e.g. Road Resurfacing - Ward 12" {...register("title")} />
            {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" rows={4} placeholder="Scope of work..." {...register("description")} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="departmentId">Department</Label>
            <DepartmentSelect
              value={departmentValue ? String(departmentValue) : ""}
              onChange={(v) => setValue("departmentId", Number(v), { shouldValidate: true })}
            />
            {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="tenderType">Tender Type</Label>
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

          <div className="space-y-2">
            <Label htmlFor="submissionDeadline">Submission Deadline</Label>
            <Input id="submissionDeadline" type="datetime-local" {...register("submissionDeadline")} />
            {errors.submissionDeadline && (
              <p className="text-sm text-destructive">{errors.submissionDeadline.message}</p>
            )}
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="files">Attachments</Label>
            <Input
              id="files"
              type="file"
              multiple
              onChange={(e) => setFiles(e.target.files ? Array.from(e.target.files) : [])}
            />
            <p className="text-xs text-muted-foreground">
              Optional. Attach requirement notes, drawings, or supporting documents.
            </p>
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
