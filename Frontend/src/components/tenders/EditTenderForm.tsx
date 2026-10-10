"use client";

import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { updateTenderSchema, type UpdateTenderFormValues } from "@/lib/validations/tender";
import { updateTender } from "@/lib/api/tenders";
import { ApiError } from "@/lib/api/client";
import type { TenderDetail } from "@/types/tender";
import { fetchTenderTypes } from "@/lib/api/lookups";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface EditTenderFormProps {
  tender: TenderDetail;
}

export function EditTenderForm({ tender }: EditTenderFormProps) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<UpdateTenderFormValues>({
    resolver: zodResolver(updateTenderSchema),
    defaultValues: {
      title: tender.title,
      description: tender.description ?? "",
      tenderType: tender.tenderType,
      estimatedAmount: tender.estimatedAmount,
      submissionDeadline: tender.submissionDeadline ?? "",
    },
  });

  const mutation = useMutation({
    mutationFn: (values: UpdateTenderFormValues) =>
      updateTender(tender.tenderId, {
        title: values.title,
        description: values.description || undefined,
        estimatedAmount: values.estimatedAmount,
        tenderType: values.tenderType,
        submissionDeadline: values.submissionDeadline || undefined,
      }),
    onSuccess: (updated) => {
      toast.success("Tender updated.");
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      queryClient.invalidateQueries({ queryKey: ["tender", tender.tenderId] });
      router.push(`/tenders/${updated.tenderId}`);
    },
    onError: (error: ApiError) => {
      toast.error(error.message || "Failed to update tender.");
    },
  });

  const tenderTypeValue = watch("tenderType");
  const isEditable = tender.currentStatus === "Draft";

  const { data: tenderTypes } = useQuery({
    queryKey: ["tender-types"],
    queryFn: fetchTenderTypes,
  });

  if (!isEditable) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">
            This tender is in <strong>{tender.currentStatus}</strong> status and can no longer be edited.
            Only tenders in <strong>Draft</strong> status are editable — this mirrors the backend rule
            (only the department officer who owns an unpublished draft can change it).
          </p>
          <Button className="mt-4" variant="outline" onClick={() => router.push(`/tenders/${tender.tenderId}`)}>
            Back to details
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Edit Tender — {tender.tenderNumber}</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => mutation.mutate(values))}
          className="grid grid-cols-1 gap-6 sm:grid-cols-2"
        >
          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="title">Title</Label>
            <Input id="title" {...register("title")} />
            {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
          </div>

          <div className="sm:col-span-2 space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" rows={4} {...register("description")} />
            {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
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
            <Input id="estimatedAmount" type="number" step="0.01" min="0.01" {...register("estimatedAmount")} />
            {errors.estimatedAmount && (
              <p className="text-sm text-destructive">{errors.estimatedAmount.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="submissionDeadline">Submission Deadline</Label>
            <Input id="submissionDeadline" type="datetime-local" {...register("submissionDeadline")} />
          </div>

          <div className="sm:col-span-2 flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={() => router.push(`/tenders/${tender.tenderId}`)} className="w-full sm:w-auto">
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || mutation.isPending} className="w-full sm:w-auto">
              {mutation.isPending ? "Saving..." : "Save Changes"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
