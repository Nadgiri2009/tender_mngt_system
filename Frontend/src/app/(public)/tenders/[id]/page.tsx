import { notFound } from "next/navigation";
import { TenderDetailView } from "@/components/tenders/TenderDetailView";

interface TenderDetailPageProps {
  params: Promise<{ id: string }>;
}

export default async function TenderDetailPage({ params }: TenderDetailPageProps) {
  const { id } = await params;
  const tenderId = Number(id);

  if (!Number.isInteger(tenderId) || tenderId <= 0) {
    notFound();
  }

  return <TenderDetailView tenderId={tenderId} />;
}
