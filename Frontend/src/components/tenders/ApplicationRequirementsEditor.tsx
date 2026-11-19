"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Save, Trash2 } from "lucide-react";
import { toast } from "sonner";

import {
  getApplicationRequirements,
  setApplicationRequirements,
  type TenderApplicationRequirement,
} from "@/lib/api/applications";
import { ApiError } from "@/lib/api/client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";

interface ApplicationRequirementsEditorProps {
  tenderId: number;
}

const blankRequirement = (): TenderApplicationRequirement => ({
  documentType: "",
  description: "",
  isMandatory: true,
  allowedExtensions: ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png",
  maxFileSizeBytes: 10_000_000,
});

export function ApplicationRequirementsEditor({ tenderId }: ApplicationRequirementsEditorProps) {
  const queryClient = useQueryClient();
  const [requirements, setRequirements] = useState<TenderApplicationRequirement[]>([]);
  const query = useQuery({
    queryKey: ["tender-application-requirements", tenderId],
    queryFn: () => getApplicationRequirements(tenderId),
  });

  useEffect(() => {
    if (query.data) setRequirements(query.data);
  }, [query.data]);

  const mutation = useMutation({
    mutationFn: () => setApplicationRequirements(tenderId, requirements),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tender-application-requirements", tenderId] });
      toast.success("Tender document requirements saved.");
    },
    onError: (error: Error) => toast.error(error instanceof ApiError ? error.message : error.message),
  });

  const updateRequirement = (index: number, patch: Partial<TenderApplicationRequirement>) => {
    setRequirements((current) => current.map((item, itemIndex) =>
      itemIndex === index ? { ...item, ...patch } : item));
  };

  return (
    <Card>
      <CardHeader><CardTitle>Application Document Requirements</CardTitle></CardHeader>
      <CardContent className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Configure the documents contractors must provide. These rules are read from SQL Server and enforced by the API.
        </p>
        {query.isLoading ? <Skeleton className="h-16 w-full" /> : (
          requirements.map((requirement, index) => (
            <div key={`${index}-${requirement.documentType}`} className="grid gap-3 rounded-md border p-3 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor={`requirement-name-${index}`}>Document name</Label>
                <Input id={`requirement-name-${index}`} maxLength={160} value={requirement.documentType}
                  onChange={(event) => updateRequirement(index, { documentType: event.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor={`requirement-description-${index}`}>Description</Label>
                <Input id={`requirement-description-${index}`} maxLength={1000} value={requirement.description ?? ""}
                  onChange={(event) => updateRequirement(index, { description: event.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor={`requirement-extensions-${index}`}>Allowed extensions</Label>
                <Input id={`requirement-extensions-${index}`} value={requirement.allowedExtensions ?? ""}
                  onChange={(event) => updateRequirement(index, { allowedExtensions: event.target.value })} />
              </div>
              <div className="flex flex-wrap items-end gap-3">
                <div className="space-y-2">
                  <Label htmlFor={`requirement-size-${index}`}>Maximum size (MB)</Label>
                  <Input id={`requirement-size-${index}`} type="number" min={1} max={10}
                    value={requirement.maxFileSizeBytes / 1_000_000}
                    onChange={(event) => updateRequirement(index, {
                      maxFileSizeBytes: Math.round(Number(event.target.value) * 1_000_000),
                    })} />
                </div>
                <label className="flex items-center gap-2 pb-2 text-sm">
                  <input type="checkbox" checked={requirement.isMandatory}
                    onChange={(event) => updateRequirement(index, { isMandatory: event.target.checked })} />
                  Mandatory
                </label>
                <Button type="button" variant="ghost" size="icon" aria-label={`Remove ${requirement.documentType || "requirement"}`}
                  onClick={() => setRequirements((current) => current.filter((_, itemIndex) => itemIndex !== index))}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          ))
        )}
        {query.isError && <p role="alert" className="text-sm text-destructive">Could not load current document requirements.</p>}
        <div className="flex flex-wrap gap-2">
          <Button type="button" variant="outline" onClick={() => setRequirements((current) => [...current, blankRequirement()])}>
            <Plus className="mr-1 h-4 w-4" /> Add Requirement
          </Button>
          <Button type="button" disabled={mutation.isPending || query.isLoading || query.isError}
            onClick={() => mutation.mutate()}>
            <Save className="mr-1 h-4 w-4" />{mutation.isPending ? "Saving..." : "Save Requirements"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
