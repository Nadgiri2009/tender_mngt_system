"use client";

import Link from "next/link";
import { LogIn, Plus } from "lucide-react";

import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { TenderTable } from "@/components/tenders/TenderTable";
import { useAuth } from "@/contexts/AuthContext";

export default function TendersPage() {
  const { user, isLoading } = useAuth();
  const canCreateTender = user?.role === "Admin" || user?.role === "DepartmentOfficer";

  return (
    <main className="mx-auto w-full max-w-[1800px] px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
      <PageHeader
        title="Tenders"
        description="Department requirements through to published tenders."
        actions={
          canCreateTender && (
            <Button asChild>
              <Link href="/tenders/new">
                <Plus className="mr-1 h-4 w-4" /> New Tender
              </Link>
            </Button>
          )
        }
      />
      {!isLoading && !user && (
        <div className="mb-4 flex flex-col gap-3 rounded-lg border border-primary/20 bg-primary/5 p-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            Showing published tenders available publicly. Sign in to view tenders available to your role; internal and draft tenders are restricted.
          </p>
          <Button asChild variant="outline" size="sm" className="shrink-0">
            <Link href="/login">
              <LogIn className="mr-1 h-4 w-4" /> Sign in
            </Link>
          </Button>
        </div>
      )}
      <TenderTable />
    </main>
  );
}
