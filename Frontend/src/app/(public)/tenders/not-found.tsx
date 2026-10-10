import Link from "next/link";
import { Button } from "@/components/ui/button";
import { FileQuestion } from "lucide-react";

export default function TenderNotFound() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-8 text-center">
      <FileQuestion className="h-12 w-12 text-muted-foreground" />
      <h2 className="text-xl font-semibold">Tender not found</h2>
      <p className="max-w-md text-sm text-muted-foreground">
        This tender doesn&apos;t exist, was deleted, or you don&apos;t have permission to view it.
      </p>
      <Button asChild>
        <Link href="/tenders">Back to Tenders</Link>
      </Button>
    </div>
  );
}
