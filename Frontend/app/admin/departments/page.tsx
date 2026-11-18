"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, Building2, Pencil, Trash2, Search, FileWarning } from "lucide-react";

import { ApiError } from "@/lib/api/client";
import { createDepartment, deleteDepartment, fetchAdminDepartments, updateDepartment } from "@/lib/api/departments";
import type { Department } from "@/types/tender";
import { formatDate } from "@/lib/utils";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";

import { PageHeader } from "@/components/layout/PageHeader";
import { PaginationControls } from "@/components/layout/PaginationControls";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
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

const departmentSchema = z.object({
  departmentName: z.string().min(2, "Department name is required").max(150),
  departmentCode: z.string().max(20).optional().or(z.literal("")),
  headOfficerName: z.string().max(150).optional().or(z.literal("")),
  isActive: z.boolean().default(true),
});
type DepartmentFormValues = z.infer<typeof departmentSchema>;

const pageSize = 10;

export default function AdminDepartmentsPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const queryClient = useQueryClient();

  const debouncedSearch = useDebouncedValue(search, 350);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["admin-departments", debouncedSearch, pageNumber, pageSize],
    queryFn: () => fetchAdminDepartments({ search: debouncedSearch, pageNumber, pageSize }),
  });
  const departments = data?.items ?? [];

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<DepartmentFormValues>({
    resolver: zodResolver(departmentSchema),
    defaultValues: { departmentName: "", departmentCode: "", headOfficerName: "", isActive: true },
  });

  const isActiveValue = watch("isActive");

  const openCreateDialog = () => {
    setEditingDepartment(null);
    reset({ departmentName: "", departmentCode: "", headOfficerName: "", isActive: true });
    setDialogOpen(true);
  };

  const openEditDialog = (department: Department) => {
    setEditingDepartment(department);
    reset({
      departmentName: department.departmentName,
      departmentCode: department.departmentCode ?? "",
      headOfficerName: department.headOfficerName ?? "",
      isActive: department.isActive ?? true,
    });
    setDialogOpen(true);
  };

  const saveMutation = useMutation({
    mutationFn: (values: DepartmentFormValues) =>
      editingDepartment
        ? updateDepartment(editingDepartment.departmentId, values)
        : createDepartment(values),
    onSuccess: () => {
      toast.success(editingDepartment ? "Department updated." : "Department added.");
      queryClient.invalidateQueries({ queryKey: ["departments"] });
      queryClient.invalidateQueries({ queryKey: ["admin-departments"] });
      setDialogOpen(false);
      setEditingDepartment(null);
      reset();
    },
    onError: (error: ApiError) =>
      toast.error(error.message || (editingDepartment ? "Failed to update department." : "Failed to add department.")),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteDepartment,
    onSuccess: (result) => {
      toast.success(result.message || "Department disabled.");
      queryClient.invalidateQueries({ queryKey: ["departments"] });
      queryClient.invalidateQueries({ queryKey: ["admin-departments"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to delete department."),
  });

  const onDelete = (department: Department) => {
    if (!window.confirm(`Delete ${department.departmentName} if it has no linked records? Linked departments will be deactivated instead.`)) return;
    deleteMutation.mutate(department.departmentId);
  };

  return (
    <div>
      <PageHeader
        title="Departments"
        description="Manage departments and the officers linked to each division."
        actions={
          <Button size="sm" onClick={openCreateDialog}>
            <Plus className="mr-1 h-4 w-4" /> Add Department
          </Button>
        }
      />

      <Card className="mb-4">
        <CardContent className="pt-6">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPageNumber(1);
              }}
              placeholder="Search departments by name, code, or officer"
              className="pl-9"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-6">
              <Skeleton className="h-52 w-full" />
            </div>
          ) : isError ? (
            <p className="p-6 text-sm text-destructive">Could not load departments.</p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>S.No</TableHead>
                    <TableHead>Department Name</TableHead>
                    <TableHead>Code</TableHead>
                    <TableHead>Officer/Head</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Created Date</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {!departments.length && (
                    <TableRow>
                      <TableCell colSpan={7} className="py-12 text-center text-sm text-muted-foreground">
                        No departments match your search.
                      </TableCell>
                    </TableRow>
                  )}

                  {departments.map((dept, index) => (
                    <TableRow key={dept.departmentId}>
                      <TableCell>{(pageNumber - 1) * pageSize + index + 1}</TableCell>
                      <TableCell className="font-medium">{dept.departmentName}</TableCell>
                      <TableCell>{dept.departmentCode || "—"}</TableCell>
                      <TableCell>{dept.headOfficerName || "—"}</TableCell>
                      <TableCell>
                        <Badge variant={dept.isActive === false ? "destructive" : "success"}>
                          {dept.isActive === false ? "Inactive" : "Active"}
                        </Badge>
                      </TableCell>
                      <TableCell>{formatDate(dept.createdDate)}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="outline" size="icon" onClick={() => openEditDialog(dept)} aria-label="Edit department" title="Edit department">
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button variant="outline" size="icon" onClick={() => onDelete(dept)} aria-label="Delete department" title="Delete or deactivate department" className="text-destructive">
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

      {isError && <p className="mt-2 text-sm text-destructive">{error instanceof Error ? error.message : "Could not load departments."}</p>}
      {data && (
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

      <Dialog open={dialogOpen} onOpenChange={(next) => {
        setDialogOpen(next);
        if (!next) {
          setEditingDepartment(null);
          reset();
        }
      }}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>{editingDepartment ? "Edit Department" : "Add Department"}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit((values) => saveMutation.mutate(values))} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="departmentName">Department Name</Label>
              <Input id="departmentName" {...register("departmentName")} />
              {errors.departmentName && <p className="text-sm text-destructive">{errors.departmentName.message}</p>}
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="departmentCode">Department Code</Label>
                <Input id="departmentCode" placeholder="e.g. PWD" {...register("departmentCode")} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="headOfficerName">Officer / Head</Label>
                <Input id="headOfficerName" placeholder="Department head or officer" {...register("headOfficerName")} />
              </div>
            </div>
            <div className="flex items-center gap-2 rounded-md border bg-muted/30 p-3">
              <input
                id="isActive"
                type="checkbox"
                checked={!!isActiveValue}
                onChange={(e) => setValue("isActive", e.target.checked, { shouldValidate: true })}
                className="h-4 w-4"
              />
              <Label htmlFor="isActive">Active department</Label>
            </div>
            <DialogFooter>
              <DialogClose asChild>
                <Button type="button" variant="outline">Cancel</Button>
              </DialogClose>
              <Button type="submit" disabled={isSubmitting || saveMutation.isPending}>
                {saveMutation.isPending ? (editingDepartment ? "Saving..." : "Adding...") : editingDepartment ? "Save Changes" : "Add Department"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
