"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/layout/PageHeader";
import { CreateTenderForm } from "@/components/tenders/CreateTenderForm";
import { useAuth } from "@/contexts/AuthContext";
import { ROLE_NAMES } from "@/types/auth";

export default function NewTenderPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && (!user || user.role !== ROLE_NAMES.JuniorEngineer)) {
      router.replace("/tenders");
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-muted-foreground">Loading...</p>
      </main>
    );
  }

  if (!user || user.role !== ROLE_NAMES.JuniorEngineer) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-destructive">Only Junior Engineer users can create tenders.</p>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <PageHeader title="New Tender" description="Create a Draft tender. Nothing here is published until submitted through the approval workflow." />
      <CreateTenderForm />
    </main>
  );
}
