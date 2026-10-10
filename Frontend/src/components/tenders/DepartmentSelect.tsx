"use client";

import { useQuery } from "@tanstack/react-query";
import { fetchDepartments } from "@/lib/api/departments";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";

interface DepartmentSelectProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

export function DepartmentSelect({ value, onChange, disabled }: DepartmentSelectProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["departments"],
    queryFn: fetchDepartments,
  });

  if (isLoading) return <Skeleton className="h-10 w-full" />;

  if (isError) {
    return <p className="text-sm text-destructive">Could not load departments.</p>;
  }

  return (
    <Select value={value} onValueChange={onChange} disabled={disabled}>
      <SelectTrigger>
        <SelectValue placeholder="Select department" />
      </SelectTrigger>
      <SelectContent>
        {data?.map((dept) => (
          <SelectItem key={dept.departmentId} value={String(dept.departmentId)}>
            {dept.departmentName}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
