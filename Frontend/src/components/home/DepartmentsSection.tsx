"use client";

import { useQuery } from "@tanstack/react-query";
import { Landmark } from "lucide-react";

import { fetchPublicDepartments } from "@/lib/api/departments";
import { Skeleton } from "@/components/ui/skeleton";

export function DepartmentsSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["departments"],
    queryFn: fetchPublicDepartments,
  });

  return (
    <section id="departments" className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <h2 className="text-2xl font-bold text-foreground">Department Directory</h2>
      <p className="mt-1 text-sm text-muted-foreground">
        Departments available in the tender portal.
      </p>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {isLoading &&
          Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}

        {isError && <p className="text-sm text-destructive">Could not load departments.</p>}

        {!isLoading && !isError && data?.length === 0 && (
          <p className="text-sm text-muted-foreground">No departments configured yet.</p>
        )}

        {data?.map((dept) => (
          <div key={dept.departmentId} className="flex items-center gap-3 rounded-lg border border-border p-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Landmark className="h-5 w-5" />
            </div>
            <p className="text-sm font-medium text-foreground">{dept.departmentName}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
