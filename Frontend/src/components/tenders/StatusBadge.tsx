import { Badge } from "@/components/ui/badge";

type BadgeVariant = "default" | "secondary" | "success" | "destructive" | "warning" | "outline";

function variantForStatus(status: string): BadgeVariant {
  if (status === "Draft") return "secondary";
  if (status === "Rejected") return "destructive";
  if (status.includes("Pending")) return "warning";
  if (
    status.includes("Approved") ||
    status === "Payment Completed" ||
    status === "Tender Published" ||
    status === "Agreement Completed"
  ) {
    return "success";
  }
  return "default";
}

export function StatusBadge({ status }: { status: string }) {
  return <Badge variant={variantForStatus(status)}>{status}</Badge>;
}
