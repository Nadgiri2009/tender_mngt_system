"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, UserCog } from "lucide-react";

import { fetchUsers, createStaffUser, toggleUserActive } from "@/lib/api/users";
import { fetchRoles } from "@/lib/api/roles";
import { fetchDepartments } from "@/lib/api/departments";
import { ApiError } from "@/lib/api/client";
import { ROLE_NAMES } from "@/types/auth";
import { formatDate } from "@/lib/utils";

import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

const staffSchema = z.object({
  fullName: z.string().min(2).max(150),
  email: z.string().email(),
  mobile: z.string().min(10).max(15),
  password: z.string().min(6),
  roleId: z.coerce.number().int().positive("Select a role"),
  departmentId: z.coerce.number().int().optional(),
});
type StaffFormValues = z.infer<typeof staffSchema>;

export default function AdminUsersPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const queryClient = useQueryClient();

  const { data: users, isLoading, isError } = useQuery({ queryKey: ["admin-users"], queryFn: fetchUsers });
  const { data: roles } = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });
  const { data: departments } = useQuery({ queryKey: ["departments"], queryFn: fetchDepartments });

  // Staff accounts should be officer/committee/admin roles -- Contractor accounts
  // come through public self-registration, not this admin form.
  const staffRoles = roles?.filter((r) => r.roleName !== ROLE_NAMES.Contractor);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<StaffFormValues>({ resolver: zodResolver(staffSchema) });

  const createMutation = useMutation({
    mutationFn: createStaffUser,
    onSuccess: () => {
      toast.success("Staff account created.");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      reset();
      setDialogOpen(false);
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to create account."),
  });

  const toggleMutation = useMutation({
    mutationFn: toggleUserActive,
    onSuccess: () => {
      toast.success("Status updated.");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to update status."),
  });

  const roleIdValue = watch("roleId");
  const departmentIdValue = watch("departmentId");

  return (
    <div>
      <PageHeader
        title="Staff Users"
        description="Officer, committee, and admin accounts. Vendor accounts self-register from the public site."
        actions={
          <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="mr-1 h-4 w-4" /> New Staff Account
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create Staff Account</DialogTitle>
              </DialogHeader>
              <form
                onSubmit={handleSubmit((v) => createMutation.mutate(v))}
                className="grid grid-cols-2 gap-3"
              >
                <div className="col-span-2 space-y-1">
                  <Label htmlFor="fullName">Full Name</Label>
                  <Input id="fullName" {...register("fullName")} />
                  {errors.fullName && <p className="text-sm text-destructive">{errors.fullName.message}</p>}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" type="email" {...register("email")} />
                  {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="mobile">Mobile</Label>
                  <Input id="mobile" {...register("mobile")} />
                  {errors.mobile && <p className="text-sm text-destructive">{errors.mobile.message}</p>}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="password">Temporary Password</Label>
                  <Input id="password" type="password" {...register("password")} />
                  {errors.password && <p className="text-sm text-destructive">{errors.password.message}</p>}
                </div>
                <div className="space-y-1">
                  <Label>Role</Label>
                  <Select
                    value={roleIdValue ? String(roleIdValue) : ""}
                    onValueChange={(v) => setValue("roleId", Number(v), { shouldValidate: true })}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select role" />
                    </SelectTrigger>
                    <SelectContent>
                      {staffRoles?.map((r) => (
                        <SelectItem key={r.roleId} value={String(r.roleId)}>
                          {r.roleName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {errors.roleId && <p className="text-sm text-destructive">{errors.roleId.message}</p>}
                </div>
                <div className="col-span-2 space-y-1">
                  <Label>Department (optional)</Label>
                  <Select
                    value={departmentIdValue ? String(departmentIdValue) : ""}
                    onValueChange={(v) => setValue("departmentId", Number(v))}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="No department" />
                    </SelectTrigger>
                    <SelectContent>
                      {departments?.map((d) => (
                        <SelectItem key={d.departmentId} value={String(d.departmentId)}>
                          {d.departmentName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <DialogFooter className="col-span-2">
                  <DialogClose asChild>
                    <Button type="button" variant="outline">Cancel</Button>
                  </DialogClose>
                  <Button type="submit" disabled={isSubmitting || createMutation.isPending}>
                    {createMutation.isPending ? "Creating..." : "Create Account"}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-6">
              <Skeleton className="h-40 w-full" />
            </div>
          ) : isError ? (
            <p className="p-6 text-sm text-destructive">Could not load users.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {users?.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} className="py-8 text-center text-sm text-muted-foreground">
                      No users yet.
                    </TableCell>
                  </TableRow>
                )}
                {users?.map((u) => (
                  <TableRow key={u.userId}>
                    <TableCell className="font-medium">{u.fullName}</TableCell>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{u.roleName}</Badge>
                    </TableCell>
                    <TableCell>{u.departmentName ?? "—"}</TableCell>
                    <TableCell>
                      <Badge variant={u.isActive ? "success" : "destructive"}>
                        {u.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(u.createdDate)}</TableCell>
                    <TableCell>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => toggleMutation.mutate(u.userId)}
                        disabled={toggleMutation.isPending}
                      >
                        <UserCog className="mr-1 h-3.5 w-3.5" />
                        {u.isActive ? "Deactivate" : "Activate"}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
