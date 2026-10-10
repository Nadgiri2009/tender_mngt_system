"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/layout/PageHeader";
import { CreateTenderForm } from "@/components/tenders/CreateTenderForm";
import { useAuth } from "@/contexts/AuthContext";

export default function NewTenderPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && (!user || (user.role !== "Admin" && user.role !== "DepartmentOfficer"))) {
      router.push("/tenders");
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-muted-foreground">Loading...</p>
      </main>
    );
  }

  if (!user || (user.role !== "Admin" && user.role !== "DepartmentOfficer")) {
    return (
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
        <p className="text-destructive">Only Admin and Department Officer users can create tenders.</p>
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
