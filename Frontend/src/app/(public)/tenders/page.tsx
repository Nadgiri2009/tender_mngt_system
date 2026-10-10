"use client";

import Link from "next/link";
import { Plus } from "lucide-react";

import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { TenderTable } from "@/components/tenders/TenderTable";
import { useAuth } from "@/contexts/AuthContext";

export default function TendersPage() {
  const { user } = useAuth();
  const canCreateTender = user?.role === "Admin" || user?.role === "DepartmentOfficer";

  return (
    <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
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
      <TenderTable />
    </main>
  );
}
