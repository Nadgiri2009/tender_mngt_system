"use client";

import { useQuery } from "@tanstack/react-query";
import { FileText, Building2, Users, CheckCircle2 } from "lucide-react";
import { fetchPublicStats } from "@/lib/api/dashboard";
import { Skeleton } from "@/components/ui/skeleton";

const STAT_CONFIG = [
  { key: "activeTenders" as const, label: "Active Tenders", icon: FileText },
  { key: "departments" as const, label: "Departments", icon: Building2 },
  { key: "registeredContractors" as const, label: "Registered Vendors", icon: Users },
  { key: "completedProjects" as const, label: "Completed Projects", icon: CheckCircle2 },
];

export function StatsSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["public-stats"],
    queryFn: fetchPublicStats,
  });

  return (
    <section className="border-y border-border bg-secondary/40">
      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-4 px-4 py-8 sm:grid-cols-2 sm:px-6 lg:grid-cols-4 lg:px-8">
        {STAT_CONFIG.map(({ key, label, icon: Icon }) => (
          <div key={key} className="flex items-center gap-4 rounded-lg bg-card p-4 shadow-sm sm:p-5">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Icon className="h-6 w-6" />
            </div>
            <div className="min-w-0">
              {isLoading && <Skeleton className="h-7 w-12" />}
              {isError && <p className="text-sm text-muted-foreground">—</p>}
              {data && <p className="text-xl font-bold text-foreground sm:text-2xl">{data[key]}</p>}
              <p className="text-xs text-muted-foreground">{label}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
