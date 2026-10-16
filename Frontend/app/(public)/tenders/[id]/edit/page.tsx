import { notFound } from "next/navigation";
import { TenderEditView } from "@/components/tenders/TenderEditView";

interface TenderEditPageProps {
  params: Promise<{ id: string }>;
}

export default async function TenderEditPage({ params }: TenderEditPageProps) {
  const { id } = await params;
  const tenderId = Number(id);

  if (!Number.isInteger(tenderId) || tenderId <= 0) {
    notFound();
  }

  return <TenderEditView tenderId={tenderId} />;
}
