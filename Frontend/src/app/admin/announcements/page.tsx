"use client";

import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, Trash2, Megaphone, Pencil } from "lucide-react";

import {
  fetchAllAnnouncements,
  createAnnouncement,
  updateAnnouncement,
  deleteAnnouncement,
  type Announcement,
} from "@/lib/api/announcements";
import { ApiError } from "@/lib/api/client";
import { formatDate } from "@/lib/utils";

import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

const announcementSchema = z.object({
  title: z.string().min(3, "Title is required").max(250),
  body: z.string().max(4000).optional().or(z.literal("")),
  isPublished: z.boolean().default(true),
  startDate: z.string().optional().or(z.literal("")),
  expiryDate: z.string().optional().or(z.literal("")),
}).refine((values) => {
  if (!values.startDate || !values.expiryDate) return true;
  return new Date(values.startDate) <= new Date(values.expiryDate);
}, {
  message: "Start date must be on or before expiry date.",
  path: ["expiryDate"],
});

type AnnouncementFormValues = z.infer<typeof announcementSchema>;

function asIsoDate(value?: string | null) {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

export default function AdminAnnouncementsPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingAnnouncement, setEditingAnnouncement] = useState<Announcement | null>(null);
  const queryClient = useQueryClient();

  const { data, isLoading, isError } = useQuery({
    queryKey: ["announcements", "all"],
    queryFn: fetchAllAnnouncements,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementSchema),
    defaultValues: { title: "", body: "", isPublished: true, startDate: "", expiryDate: "" },
  });

  const openCreateDialog = () => {
    setEditingAnnouncement(null);
    reset({ title: "", body: "", isPublished: true, startDate: "", expiryDate: "" });
    setDialogOpen(true);
  };

  const openEditDialog = (announcement: Announcement) => {
    setEditingAnnouncement(announcement);
    reset({
      title: announcement.title,
      body: announcement.body ?? "",
      isPublished: announcement.isPublished,
      startDate: asIsoDate(announcement.startDate ?? announcement.publishDate),
      expiryDate: asIsoDate(announcement.expiryDate),
    });
    setDialogOpen(true);
  };

  const saveMutation = useMutation({
    mutationFn: (values: AnnouncementFormValues) => {
      const payload = {
        title: values.title,
        body: values.body || undefined,
        isPublished: values.isPublished,
        startDate: values.startDate ? new Date(`${values.startDate}T00:00:00Z`).toISOString() : undefined,
        publishDate: values.startDate ? new Date(`${values.startDate}T00:00:00Z`).toISOString() : undefined,
        expiryDate: values.expiryDate ? new Date(`${values.expiryDate}T00:00:00Z`).toISOString() : undefined,
      };

      return editingAnnouncement
        ? updateAnnouncement(editingAnnouncement.announcementId, payload)
        : createAnnouncement(payload);
    },
    onSuccess: () => {
      toast.success(editingAnnouncement ? "Announcement updated." : "Announcement created.");
      queryClient.invalidateQueries({ queryKey: ["announcements"] });
      reset();
      setEditingAnnouncement(null);
      setDialogOpen(false);
    },
    onError: (error: ApiError) =>
      toast.error(error.message || (editingAnnouncement ? "Failed to update announcement." : "Failed to create announcement.")),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteAnnouncement,
    onSuccess: () => {
      toast.success("Announcement removed.");
      queryClient.invalidateQueries({ queryKey: ["announcements"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to remove announcement."),
  });

  return (
    <div>
      <PageHeader
        title="Announcements"
        description="Shown in the Important Announcements section on the public homepage."
        actions={
          <Dialog open={dialogOpen} onOpenChange={(next) => {
            setDialogOpen(next);
            if (!next) {
              setEditingAnnouncement(null);
              reset();
            }
          }}>
            <DialogTrigger asChild>
              <Button size="sm" onClick={openCreateDialog}>
                <Plus className="mr-1 h-4 w-4" /> New Announcement
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-xl">
              <DialogHeader>
                <DialogTitle>{editingAnnouncement ? "Edit Announcement" : "New Announcement"}</DialogTitle>
              </DialogHeader>
              <form onSubmit={handleSubmit((v) => saveMutation.mutate(v))} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="title">Title</Label>
                  <Input id="title" {...register("title")} />
                  {errors.title && <p className="text-sm text-destructive">{errors.title.message}</p>}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="body">Body</Label>
                  <Textarea id="body" rows={4} {...register("body")} />
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="startDate">Announcement Start Date</Label>
                    <Input id="startDate" type="date" {...register("startDate")} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="expiryDate">Expiry Date (optional)</Label>
                    <Input id="expiryDate" type="date" {...register("expiryDate")} />
                  </div>
                </div>
                {errors.expiryDate && <p className="text-sm text-destructive">{errors.expiryDate.message}</p>}
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" {...register("isPublished")} className="h-4 w-4" />
                  Publish immediately
                </label>
                <DialogFooter>
                  <DialogClose asChild>
                    <Button type="button" variant="outline">Cancel</Button>
                  </DialogClose>
                  <Button type="submit" disabled={isSubmitting || saveMutation.isPending}>
                    {saveMutation.isPending ? (editingAnnouncement ? "Saving..." : "Creating...") : editingAnnouncement ? "Save Changes" : "Create"}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      <div className="space-y-3">
        {isLoading && Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}
        {isError && <p className="text-sm text-destructive">Could not load announcements.</p>}
        {!isLoading && !isError && data?.length === 0 && (
          <p className="text-sm text-muted-foreground">No announcements yet.</p>
        )}
        {data?.map((a: Announcement) => (
          <Card key={a.announcementId}>
            <CardContent className="flex items-start justify-between gap-4 pt-6">
              <div className="flex items-start gap-3">
                <Megaphone className="mt-0.5 h-5 w-5 shrink-0 text-accent" />
                <div>
                  <div className="flex items-center gap-2">
                    <p className="font-medium text-foreground">{a.title}</p>
                    <Badge variant={a.isPublished ? "success" : "secondary"}>
                      {a.isPublished ? "Published" : "Draft"}
                    </Badge>
                  </div>
                  {a.body && <p className="mt-1 text-sm text-muted-foreground">{a.body}</p>}
                  <div className="mt-2 flex flex-wrap gap-2 text-xs text-muted-foreground">
                    <span>Start: {formatDate(a.startDate ?? a.publishDate)}</span>
                    <span>•</span>
                    <span>Expiry: {formatDate(a.expiryDate)}</span>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    By {a.createdByName || "—"} on {formatDate(a.createdDate)}
                  </p>
                </div>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="icon" onClick={() => openEditDialog(a)} aria-label="Edit announcement">
                  <Pencil className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => deleteMutation.mutate(a.announcementId)}
                  disabled={deleteMutation.isPending}
                  aria-label="Delete announcement"
                  className="text-destructive"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
