"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Trash2 } from "lucide-react";

import { deleteTender } from "@/lib/api/tenders";
import { ApiError } from "@/lib/api/client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

interface DeleteTenderDialogProps {
  tenderId: number;
  tenderNumber: string;
}

export function DeleteTenderDialog({ tenderId, tenderNumber }: DeleteTenderDialogProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);

  const mutation = useMutation({
    mutationFn: () => deleteTender(tenderId),
    onSuccess: () => {
      toast.success(`Tender ${tenderNumber} deleted.`);
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      setOpen(false);
      router.push("/tenders");
    },
    onError: (error: ApiError) => {
      toast.error(error.message || "Failed to delete tender.");
    },
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="destructive" size="sm">
          <Trash2 className="mr-1 h-4 w-4" /> Delete
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete {tenderNumber}?</DialogTitle>
          <DialogDescription>
            This is a soft delete — the record is preserved for audit purposes and marked deleted, but this
            action cannot be undone from this screen. Only Draft tenders can be deleted.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">Cancel</Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending ? "Deleting..." : "Delete Tender"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
