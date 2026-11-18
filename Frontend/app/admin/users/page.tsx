"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { KeyRound, Pencil, Plus, Search, Trash2, UserCog } from "lucide-react";

import {
  createStaffUser,
  deleteUser,
  fetchUsersPage,
  resetUserPassword,
  toggleUserActive,
  updateStaffUser,
  type AdminUser,
} from "@/lib/api/users";
import { fetchRoles } from "@/lib/api/roles";
import { fetchDepartments } from "@/lib/api/departments";
import { ApiError } from "@/lib/api/client";
import { formatDate } from "@/lib/utils";

import { PageHeader } from "@/components/layout/PageHeader";
import { PaginationControls } from "@/components/layout/PaginationControls";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
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
  mobile: z.string().regex(/^\d{10}$/, "Enter a 10-digit mobile number."),
  password: z.string().min(6),
  roleId: z.coerce.number().int().positive("Select a role"),
  departmentId: z.coerce.number().int().positive().optional(),
});
type StaffFormValues = z.infer<typeof staffSchema>;

const editStaffSchema = z.object({
  fullName: z.string().min(2).max(150),
  email: z.string().email(),
  mobile: z.string().regex(/^\d{10}$/, "Enter a 10-digit mobile number."),
  roleId: z.coerce.number().int().positive("Select a role"),
  departmentId: z.coerce.number().int().positive().optional(),
});
type EditStaffFormValues = z.infer<typeof editStaffSchema>;

const pageSize = 10;

export default function AdminUsersPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [passwordResetUser, setPasswordResetUser] = useState<AdminUser | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const queryClient = useQueryClient();
  const debouncedSearch = useDebouncedValue(search, 350);

  const { data: userPage, isLoading, isError, error } = useQuery({
    queryKey: ["admin-user-pages", debouncedSearch, pageNumber, pageSize],
    queryFn: () => fetchUsersPage({ search: debouncedSearch, pageNumber, pageSize }),
  });
  const { data: roles } = useQuery({ queryKey: ["roles"], queryFn: fetchRoles });
  const { data: departments } = useQuery({ queryKey: ["departments"], queryFn: fetchDepartments });
  const users = userPage?.items ?? [];

  // Staff accounts should be officer/committee/admin roles -- Contractor accounts
  // come through public self-registration, not this admin form.
  const staffRoles = roles?.filter((role) =>
    ["Junior Engineer (JE)", "TENDER_CELL", "CHAIRMAN", "DIRECTOR_AUDITOR"].includes(role.roleCode)
  ) ?? [];

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<StaffFormValues>({ resolver: zodResolver(staffSchema) });

  const {
    register: registerEdit,
    handleSubmit: handleEditSubmit,
    setValue: setEditValue,
    watch: watchEdit,
    reset: resetEdit,
    formState: { errors: editErrors, isSubmitting: isEditSubmitting },
  } = useForm<EditStaffFormValues>({ resolver: zodResolver(editStaffSchema) });

  const createMutation = useMutation({
    mutationFn: createStaffUser,
    onSuccess: () => {
      toast.success("Staff account created.");
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      queryClient.invalidateQueries({ queryKey: ["admin-user-pages"] });
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
      queryClient.invalidateQueries({ queryKey: ["admin-user-pages"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to update status."),
  });

  const editMutation = useMutation({
    mutationFn: (values: EditStaffFormValues) => {
      if (!editingUser) throw new Error("Select a user to edit.");
      return updateStaffUser(editingUser.userId, values);
    },
    onSuccess: () => {
      toast.success("User details updated.");
      queryClient.invalidateQueries({ queryKey: ["admin-user-pages"] });
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
      setEditingUser(null);
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to update user."),
  });

  const passwordMutation = useMutation({
    mutationFn: () => {
      if (!passwordResetUser) throw new Error("Select a user to reset.");
      return resetUserPassword(passwordResetUser.userId, newPassword);
    },
    onSuccess: () => {
      toast.success("Password reset.");
      setPasswordResetUser(null);
      setNewPassword("");
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to reset password."),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteUser,
    onSuccess: () => {
      toast.success("User deleted.");
      queryClient.invalidateQueries({ queryKey: ["admin-user-pages"] });
      queryClient.invalidateQueries({ queryKey: ["admin-users"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "This user could not be deleted."),
  });

  const roleIdValue = watch("roleId");
  const departmentIdValue = watch("departmentId");
  const editRoleIdValue = watchEdit("roleId");
  const editDepartmentIdValue = watchEdit("departmentId");

  const openEditDialog = (user: AdminUser) => {
    setEditingUser(user);
    const role = staffRoles.find((item) => item.roleName === user.roleName);
    const department = departments?.find((item) => item.departmentName === user.departmentName);
    resetEdit({
      fullName: user.fullName,
      email: user.email,
      mobile: user.mobile ?? "",
      roleId: role?.roleId ?? 0,
      departmentId: department?.departmentId,
    });
  };

  const onDelete = (user: AdminUser) => {
    if (!window.confirm(`Permanently remove ${user.fullName} if no business or audit history references the account?`)) return;
    deleteMutation.mutate(user.userId);
  };

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
            <DialogContent className="max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Create Staff Account</DialogTitle>
              </DialogHeader>
              <form
                onSubmit={handleSubmit((v) => createMutation.mutate(v))}
                className="grid grid-cols-1 gap-3 sm:grid-cols-2"
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
              placeholder="Search by name, email, mobile, role, or department"
              className="pl-9"
              aria-label="Search users"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-6">
              <Skeleton className="h-40 w-full" />
            </div>
          ) : isError ? (
            <p className="p-6 text-sm text-destructive">{error instanceof Error ? error.message : "Could not load users."}</p>
          ) : (
            <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Sr. No.</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Account Type</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} className="py-8 text-center text-sm text-muted-foreground">
                      No users match your search.
                    </TableCell>
                  </TableRow>
                )}
                {users.map((u, index) => (
                  <TableRow key={u.userId}>
                    <TableCell>{(pageNumber - 1) * pageSize + index + 1}</TableCell>
                    <TableCell className="font-medium">{u.fullName}</TableCell>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>{u.accountType === "Internal" ? "Internal" : "External bidder"}</TableCell>
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
                      <div className="flex justify-end gap-1">
                        {u.accountType === "Internal" && (
                          <Button variant="outline" size="icon" title="Edit user" aria-label="Edit user" onClick={() => openEditDialog(u)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                        )}
                        <Button
                          variant="outline"
                          size="icon"
                          title={u.isActive ? "Deactivate user" : "Activate user"}
                          aria-label={u.isActive ? "Deactivate user" : "Activate user"}
                          onClick={() => toggleMutation.mutate(u.userId)}
                          disabled={toggleMutation.isPending}
                        >
                          <UserCog className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="icon"
                          title="Reset password"
                          aria-label="Reset password"
                          onClick={() => {
                            setPasswordResetUser(u);
                            setNewPassword("");
                          }}
                        >
                          <KeyRound className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="icon"
                          title="Delete user if unreferenced"
                          aria-label="Delete user if unreferenced"
                          className="text-destructive"
                          onClick={() => onDelete(u)}
                          disabled={deleteMutation.isPending}
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

      {userPage && (
        <div className="mt-4">
          <PaginationControls
            pageNumber={userPage.pageNumber}
            pageSize={userPage.pageSize}
            totalCount={userPage.totalCount}
            totalPages={userPage.totalPages}
            onPageChange={setPageNumber}
          />
        </div>
      )}

      <Dialog open={editingUser !== null} onOpenChange={(open) => {
        if (!open) setEditingUser(null);
      }}>
        <DialogContent className="max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Edit Staff User</DialogTitle></DialogHeader>
          <form onSubmit={handleEditSubmit((values) => editMutation.mutate(values))} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="editFullName">Full Name</Label>
              <Input id="editFullName" {...registerEdit("fullName")} />
              {editErrors.fullName && <p className="text-sm text-destructive">{editErrors.fullName.message}</p>}
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="editEmail">Email</Label>
                <Input id="editEmail" type="email" {...registerEdit("email")} />
                {editErrors.email && <p className="text-sm text-destructive">{editErrors.email.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="editMobile">Mobile</Label>
                <Input id="editMobile" inputMode="numeric" {...registerEdit("mobile")} />
                {editErrors.mobile && <p className="text-sm text-destructive">{editErrors.mobile.message}</p>}
              </div>
            </div>
            <div className="space-y-2">
              <Label>Role</Label>
              <Select value={editRoleIdValue ? String(editRoleIdValue) : ""} onValueChange={(value) => setEditValue("roleId", Number(value), { shouldValidate: true })}>
                <SelectTrigger><SelectValue placeholder="Select role" /></SelectTrigger>
                <SelectContent>
                  {staffRoles.map((role) => <SelectItem key={role.roleId} value={String(role.roleId)}>{role.roleName}</SelectItem>)}
                </SelectContent>
              </Select>
              {editErrors.roleId && <p className="text-sm text-destructive">{editErrors.roleId.message}</p>}
            </div>
            <div className="space-y-2">
              <Label>Department (optional)</Label>
              <Select value={editDepartmentIdValue ? String(editDepartmentIdValue) : "none"} onValueChange={(value) => setEditValue("departmentId", value === "none" ? undefined : Number(value))}>
                <SelectTrigger><SelectValue placeholder="Select department" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No department</SelectItem>
                  {departments?.map((department) => <SelectItem key={department.departmentId} value={String(department.departmentId)}>{department.departmentName}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditingUser(null)}>Cancel</Button>
              <Button type="submit" disabled={isEditSubmitting || editMutation.isPending}>
                {editMutation.isPending ? "Saving..." : "Save Changes"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={passwordResetUser !== null} onOpenChange={(open) => {
        if (!open) setPasswordResetUser(null);
      }}>
        <DialogContent>
          <DialogHeader><DialogTitle>Reset User Password</DialogTitle></DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="newPassword">Temporary password</Label>
            <Input id="newPassword" type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
            <p className="text-xs text-muted-foreground">Use at least 6 characters.</p>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setPasswordResetUser(null)}>Cancel</Button>
            <Button
              type="button"
              disabled={newPassword.length < 6 || passwordMutation.isPending}
              onClick={() => passwordMutation.mutate()}
            >
              {passwordMutation.isPending ? "Resetting..." : "Reset Password"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
