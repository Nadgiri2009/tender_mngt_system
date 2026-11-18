"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { FileWarning, Pencil, Plus, Search, Trash2 } from "lucide-react";

import { ApiError } from "@/lib/api/client";
import {
  createMasterDataItem,
  deleteMasterDataItem,
  fetchMasterData,
  updateMasterDataItem,
  type MasterDataCatalog,
  type MasterDataItem,
  type SaveMasterDataItemInput,
} from "@/lib/api/masterData";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";

import { PageHeader } from "@/components/layout/PageHeader";
import { PaginationControls } from "@/components/layout/PaginationControls";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Dialog, DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const CATALOGS: Record<string, {
  apiCatalog: MasterDataCatalog;
  title: string;
  description: string;
  nameLabel: string;
  maxNameLength: number;
}> = {
  "tender-methods": {
    apiCatalog: "tender-methods",
    title: "Tender Methods",
    description: "Manage the active tender methods available during tender creation.",
    nameLabel: "Method name",
    maxNameLength: 50,
  },
  "tender-categories": {
    apiCatalog: "tender-types",
    title: "Tender Categories",
    description: "Manage the active tender categories available during tender creation.",
    nameLabel: "Category name",
    maxNameLength: 50,
  },
  "document-types": {
    apiCatalog: "document-types",
    title: "Document Types",
    description: "Manage document types used for tender and bid requirements.",
    nameLabel: "Document type",
    maxNameLength: 80,
  },
  units: {
    apiCatalog: "units",
    title: "Units of Measurement",
    description: "Manage units used by bill-of-quantities items.",
    nameLabel: "Unit name",
    maxNameLength: 20,
  },
};

const PAGE_SIZE = 10;

export default function MasterDataCatalogPage() {
  const params = useParams<{ catalog: string }>();
  const routeCatalog = params?.catalog ?? "";
  const catalog = CATALOGS[routeCatalog];
  const [dialogOpen, setDialogOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editingItem, setEditingItem] = useState<MasterDataItem | null>(null);
  const [form, setForm] = useState<SaveMasterDataItemInput>({
    name: "",
    sortOrder: 0,
    isActive: true,
  });
  const queryClient = useQueryClient();
  const debouncedSearch = useDebouncedValue(search, 350);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["master-data", catalog?.apiCatalog, debouncedSearch, pageNumber, PAGE_SIZE],
    queryFn: () => fetchMasterData(catalog!.apiCatalog, {
      search: debouncedSearch,
      pageNumber,
      pageSize: PAGE_SIZE,
    }),
    enabled: Boolean(catalog),
  });

  const saveMutation = useMutation({
    mutationFn: (input: SaveMasterDataItemInput) =>
      editingItem
        ? updateMasterDataItem(catalog!.apiCatalog, editingItem.name, input)
        : createMasterDataItem(catalog!.apiCatalog, input),
    onSuccess: () => {
      toast.success(editingItem ? "Master-data item updated." : "Master-data item added.");
      queryClient.invalidateQueries({ queryKey: ["master-data", catalog?.apiCatalog] });
      if (catalog?.apiCatalog === "tender-methods" || catalog?.apiCatalog === "tender-types") {
        queryClient.invalidateQueries({ queryKey: ["tender-types"] });
        queryClient.invalidateQueries({ queryKey: ["tender-categories"] });
      }
      setDialogOpen(false);
      setEditingItem(null);
    },
    onError: (saveError: ApiError) => toast.error(saveError.message || "Could not save this item."),
  });

  const deleteMutation = useMutation({
    mutationFn: (key: string) => deleteMasterDataItem(catalog!.apiCatalog, key),
    onSuccess: (result) => {
      toast.success(result.message || "Master-data item deleted.");
      queryClient.invalidateQueries({ queryKey: ["master-data", catalog?.apiCatalog] });
      if (catalog?.apiCatalog === "tender-methods" || catalog?.apiCatalog === "tender-types") {
        queryClient.invalidateQueries({ queryKey: ["tender-types"] });
        queryClient.invalidateQueries({ queryKey: ["tender-categories"] });
      }
    },
    onError: (deleteError: ApiError) => toast.error(deleteError.message || "Could not delete this item."),
  });

  const openCreateDialog = () => {
    setEditingItem(null);
    setForm({ name: "", sortOrder: 0, isActive: true });
    setDialogOpen(true);
  };

  const openEditDialog = (item: MasterDataItem) => {
    setEditingItem(item);
    setForm({ name: item.name, sortOrder: item.sortOrder, isActive: item.isActive });
    setDialogOpen(true);
  };

  const save = () => {
    if (!catalog) return;
    const name = form.name.trim();
    if (!name) {
      toast.error(`${catalog?.nameLabel ?? "Name"} is required.`);
      return;
    }
    if (name.length > catalog.maxNameLength) {
      toast.error(`Name must be ${catalog.maxNameLength} characters or fewer.`);
      return;
    }
    if (!Number.isInteger(form.sortOrder) || form.sortOrder < 0 || form.sortOrder > 100000) {
      toast.error("Sort order must be a whole number between 0 and 100000.");
      return;
    }
    saveMutation.mutate({ ...form, name });
  };

  const confirmDelete = (item: MasterDataItem) => {
    if (!window.confirm(`Delete "${item.name}" if it is unused? Referenced items will be deactivated.`)) {
      return;
    }
    deleteMutation.mutate(item.name);
  };

  if (!catalog) {
    return (
      <div className="space-y-3">
        <PageHeader title="Master Data" description="This master-data catalog is not available." />
        <p className="text-sm text-destructive">The requested catalog is not supported.</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={catalog.title}
        description={catalog.description}
        actions={
          <Button size="sm" onClick={openCreateDialog}>
            <Plus className="mr-1 h-4 w-4" /> Add {catalog.nameLabel}
          </Button>
        }
      />

      <Card className="mb-4">
        <CardContent className="pt-6">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPageNumber(1);
              }}
              placeholder={`Search ${catalog.title.toLowerCase()}...`}
              className="pl-9"
              aria-label={`Search ${catalog.title}`}
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-6"><Skeleton className="h-52 w-full" /></div>
          ) : isError ? (
            <div className="flex items-center gap-2 p-6 text-sm text-destructive">
              <FileWarning className="h-5 w-5 shrink-0" />
              <span>{error instanceof Error ? error.message : `Could not load ${catalog.title.toLowerCase()}.`}</span>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Sr. No.</TableHead>
                    <TableHead>{catalog.nameLabel}</TableHead>
                    <TableHead>Sort Order</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {!data?.items.length && (
                    <TableRow>
                      <TableCell colSpan={5} className="py-12 text-center text-sm text-muted-foreground">
                        No {catalog.title.toLowerCase()} match your search.
                      </TableCell>
                    </TableRow>
                  )}
                  {data?.items.map((item, index) => (
                    <TableRow key={item.name}>
                      <TableCell>{(pageNumber - 1) * PAGE_SIZE + index + 1}</TableCell>
                      <TableCell className="font-medium">{item.name}</TableCell>
                      <TableCell>{item.sortOrder}</TableCell>
                      <TableCell>
                        <Badge variant={item.isActive ? "success" : "destructive"}>
                          {item.isActive ? "Active" : "Inactive"}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="icon"
                            aria-label={`Edit ${item.name}`}
                            title={`Edit ${item.name}`}
                            onClick={() => openEditDialog(item)}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            type="button"
                            variant="outline"
                            size="icon"
                            className="text-destructive"
                            aria-label={`Delete ${item.name}`}
                            title={`Delete ${item.name}`}
                            disabled={deleteMutation.isPending}
                            onClick={() => confirmDelete(item)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {data?.totalCount === 0 && (
        <p className="mt-4 text-sm text-muted-foreground">0 records</p>
      )}
      {data && data.totalCount > 0 && (
        <div className="mt-4">
          <PaginationControls
            pageNumber={data.pageNumber}
            pageSize={data.pageSize}
            totalCount={data.totalCount}
            totalPages={data.totalPages}
            onPageChange={setPageNumber}
          />
        </div>
      )}

      <Dialog
        open={dialogOpen}
        onOpenChange={(open) => {
          setDialogOpen(open);
          if (!open) setEditingItem(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingItem ? `Edit ${catalog.nameLabel}` : `Add ${catalog.nameLabel}`}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="master-data-name">{catalog.nameLabel}</Label>
              <Input
                id="master-data-name"
                maxLength={catalog.maxNameLength}
                value={form.name}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="master-data-sort-order">Sort Order</Label>
              <Input
                id="master-data-sort-order"
                type="number"
                min={0}
                max={100000}
                step={1}
                value={form.sortOrder}
                onChange={(event) => setForm((current) => ({
                  ...current,
                  sortOrder: event.target.value === "" ? 0 : Number(event.target.value),
                }))}
              />
            </div>
            <label className="flex items-center gap-2 rounded-md border bg-muted/30 p-3 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))}
                className="h-4 w-4"
              />
              Active
            </label>
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">Cancel</Button>
            </DialogClose>
            <Button type="button" onClick={save} disabled={saveMutation.isPending}>
              {saveMutation.isPending ? "Saving..." : editingItem ? "Save Changes" : "Add Item"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
