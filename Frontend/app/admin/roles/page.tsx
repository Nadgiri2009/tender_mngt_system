"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Save, ShieldCheck } from "lucide-react";

import { fetchRolePermissionMatrix, updateRolePermissions } from "@/lib/api/roles";
import { ApiError } from "@/lib/api/client";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

const protectedjePermissions = new Set(["users.manage", "masterdata.manage", "roles.manage"]);

export default function AdminRolesPage() {
  const queryClient = useQueryClient();
  const [selections, setSelections] = useState<Record<number, Set<string>>>({});
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["role-permission-matrix"],
    queryFn: fetchRolePermissionMatrix,
  });

  useEffect(() => {
    if (data) {
      setSelections(Object.fromEntries(
        data.roles.map((role) => [role.roleId, new Set(role.permissionKeys)])
      ));
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: ({ roleId, permissionKeys }: { roleId: number; permissionKeys: string[] }) =>
      updateRolePermissions(roleId, permissionKeys),
    onSuccess: () => {
      toast.success("Role permissions updated.");
      queryClient.invalidateQueries({ queryKey: ["role-permission-matrix"] });
    },
    onError: (saveError: ApiError) => toast.error(saveError.message || "Failed to update role permissions."),
  });

  const togglePermission = (roleId: number, permissionKey: string, checked: boolean) => {
    setSelections((current) => {
      const next = new Set(current[roleId] ?? []);
      if (checked) next.add(permissionKey);
      else next.delete(permissionKey);
      return { ...current, [roleId]: next };
    });
  };

  return (
    <div>
      <PageHeader
        title="Roles & Permissions"
        description="Manage module access for the four fixed internal roles. Roles cannot be created or removed."
      />

      <div className="mb-4 rounded-lg border border-primary/20 bg-primary/5 p-4 text-sm">
        <p className="font-medium">Junior Engineer is the Main Admin.</p>
        <p className="mt-1 text-muted-foreground">
          The user, master-data, and role-management permissions remain protected so the portal cannot lose its administrator.
        </p>
      </div>

      {isLoading && <Skeleton className="h-72 w-full" />}
      {isError && (
        <p role="alert" className="text-sm text-destructive">
          {error instanceof Error ? error.message : "Could not load role permissions."}
        </p>
      )}

      {data && (
        <div className="grid gap-4 xl:grid-cols-2">
          {data.roles.map((role) => {
            const selected = selections[role.roleId] ?? new Set(role.permissionKeys);
            return (
              <Card key={role.roleId}>
                <CardHeader className="flex flex-row items-start justify-between gap-3">
                  <div>
                    <CardTitle className="flex items-center gap-2 text-base">
                      <ShieldCheck className="h-4 w-4 text-primary" />
                      {role.roleName}
                    </CardTitle>
                    <p className="mt-1 text-xs text-muted-foreground">{role.roleCode}</p>
                  </div>
                  <Button
                    size="sm"
                    onClick={() => saveMutation.mutate({
                      roleId: role.roleId,
                      permissionKeys: [...selected].sort(),
                    })}
                    disabled={saveMutation.isPending}
                  >
                    <Save className="mr-1 h-4 w-4" />
                    Save
                  </Button>
                </CardHeader>
                <CardContent className="space-y-2">
                  {data.permissions.map((permission) => {
                    const checked = selected.has(permission.permissionKey);
                    const isProtected = role.roleCode === "Junior Engineer (JE)" && protectedjePermissions.has(permission.permissionKey);
                    return (
                      <label key={permission.permissionKey} className="flex items-start gap-3 rounded-md border p-3">
                        <input
                          type="checkbox"
                          className="mt-1 h-4 w-4 shrink-0"
                          checked={checked}
                          disabled={isProtected || saveMutation.isPending}
                          aria-label={`${permission.permissionKey} for ${role.roleName}`}
                          onChange={(event) => togglePermission(role.roleId, permission.permissionKey, event.target.checked)}
                        />
                        <span className="min-w-0">
                          <span className="block break-all text-sm font-medium">{permission.permissionKey}</span>
                          <span className="mt-1 block text-xs text-muted-foreground">{permission.description}</span>
                        </span>
                      </label>
                    );
                  })}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
