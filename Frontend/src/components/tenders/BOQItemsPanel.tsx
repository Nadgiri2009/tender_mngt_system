"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, Trash2 } from "lucide-react";

import { boqItemSchema, type BOQItemFormValues } from "@/lib/validations/tender";
import { addBOQItem, deleteBOQItem } from "@/lib/api/boq";
import { ApiError } from "@/lib/api/client";
import type { BOQItem } from "@/types/tender";
import { formatCurrencyINR } from "@/lib/utils";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

interface BOQItemsPanelProps {
  tenderId: number;
  items: BOQItem[];
  editable: boolean;
}

export function BOQItemsPanel({ tenderId, items, editable }: BOQItemsPanelProps) {
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<BOQItemFormValues>({
    resolver: zodResolver(boqItemSchema),
    defaultValues: { itemName: "", description: "", quantity: 0, unit: "", rate: 0 },
  });

  const addMutation = useMutation({
    mutationFn: (values: BOQItemFormValues) => addBOQItem(tenderId, values),
    onSuccess: () => {
      toast.success("BOQ item added.");
      queryClient.invalidateQueries({ queryKey: ["tender", tenderId] });
      reset();
      setDialogOpen(false);
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to add item."),
  });

  const deleteMutation = useMutation({
    mutationFn: (boqId: number) => deleteBOQItem(boqId),
    onSuccess: () => {
      toast.success("BOQ item removed.");
      queryClient.invalidateQueries({ queryKey: ["tender", tenderId] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to remove item."),
  });

  const total = items.reduce((sum, item) => sum + item.amount, 0);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground">Bill of Quantities</h3>
        {editable && (
          <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm" variant="outline">
                <Plus className="mr-1 h-4 w-4" /> Add Item
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Add BOQ Item</DialogTitle>
              </DialogHeader>
              <form
                onSubmit={handleSubmit((values) => addMutation.mutate(values))}
                className="grid grid-cols-2 gap-3"
              >
                <div className="col-span-2 space-y-1">
                  <label className="text-sm font-medium">Item Name</label>
                  <Input {...register("itemName")} />
                  {errors.itemName && <p className="text-sm text-destructive">{errors.itemName.message}</p>}
                </div>
                <div className="col-span-2 space-y-1">
                  <label className="text-sm font-medium">Description</label>
                  <Input {...register("description")} />
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Quantity</label>
                  <Input type="number" step="0.001" {...register("quantity")} />
                  {errors.quantity && <p className="text-sm text-destructive">{errors.quantity.message}</p>}
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Unit</label>
                  <Input placeholder="e.g. sqm, kg, nos" {...register("unit")} />
                  {errors.unit && <p className="text-sm text-destructive">{errors.unit.message}</p>}
                </div>
                <div className="space-y-1">
                  <label className="text-sm font-medium">Rate (₹)</label>
                  <Input type="number" step="0.01" {...register("rate")} />
                  {errors.rate && <p className="text-sm text-destructive">{errors.rate.message}</p>}
                </div>
                <DialogFooter className="col-span-2 mt-2">
                  <DialogClose asChild>
                    <Button type="button" variant="outline">
                      Cancel
                    </Button>
                  </DialogClose>
                  <Button type="submit" disabled={isSubmitting || addMutation.isPending}>
                    {addMutation.isPending ? "Adding..." : "Add Item"}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">No BOQ items added yet.</p>
      ) : (
        <div className="rounded-lg border border-border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Item</TableHead>
                <TableHead>Unit</TableHead>
                <TableHead className="text-right">Qty</TableHead>
                <TableHead className="text-right">Rate</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                {editable && <TableHead className="w-12" />}
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.boqId}>
                  <TableCell>
                    <div className="font-medium">{item.itemName}</div>
                    {item.description && (
                      <div className="text-xs text-muted-foreground">{item.description}</div>
                    )}
                  </TableCell>
                  <TableCell>{item.unit}</TableCell>
                  <TableCell className="text-right tabular-nums">{item.quantity}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatCurrencyINR(item.rate)}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatCurrencyINR(item.amount)}</TableCell>
                  {editable && (
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => deleteMutation.mutate(item.boqId)}
                        disabled={deleteMutation.isPending}
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </TableCell>
                  )}
                </TableRow>
              ))}
              <TableRow>
                <TableCell colSpan={editable ? 4 : 4} className="text-right font-semibold">
                  Total
                </TableCell>
                <TableCell className="text-right font-semibold tabular-nums">
                  {formatCurrencyINR(total)}
                </TableCell>
                {editable && <TableCell />}
              </TableRow>
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
