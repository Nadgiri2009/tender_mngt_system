import Link from "next/link";

import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

const TERMS = [
  {
    title: "Tender notice and amendments",
    detail: "Read the complete tender notice, schedules, specifications, and every addendum before preparing a bid. The tender-specific documents and later official amendments govern if this general summary differs from them.",
  },
  {
    title: "Eligibility and registration",
    detail: "Bidders must maintain accurate portal registration details and meet the eligibility, licensing, and qualification criteria stated in the relevant tender. Registration on the portal does not guarantee eligibility or award.",
  },
  {
    title: "Preparation and submission",
    detail: "Submit complete, legible, correctly signed information through the method and formats stated in the tender. Upload technical and financial information in the specified sections; do not include price information in a technical submission where a separate-envelope process applies.",
  },
  {
    title: "Deadlines and bid validity",
    detail: "Plan for upload and submission completion before the published closing time. A submission is subject to the time recorded by the portal. Bid validity, extensions, and any permitted withdrawal or modification are governed by the tender notice.",
  },
  {
    title: "Fees and bid security",
    detail: "Pay tender fees, earnest money deposit, or other security only where required, in the amount, form, and manner specified in the tender. Any exemption must be supported by valid documents and is subject to the stated eligibility rules.",
  },
  {
    title: "Evaluation and clarification",
    detail: "Bids are examined against the published eligibility, technical, and commercial criteria. The Corporation may seek clarifications where permitted by the tender and applicable rules; a clarification must not be treated as permission to change a bid unless expressly allowed.",
  },
  {
    title: "Integrity and accuracy",
    detail: "Information and documents submitted must be genuine, current, and complete. Bidders must avoid collusion, misrepresentation, unauthorized access, and any attempt to influence the process outside the official channels. Consequences are governed by the tender and applicable rules.",
  },
  {
    title: "Award and contract",
    detail: "An online notice or evaluation result does not by itself create a contract. The successful bidder must complete the acceptance, agreement, performance security, insurance, and other post-award requirements specified in the tender.",
  },
  {
    title: "Portal use and account security",
    detail: "Keep login credentials and signing credentials secure, use supported file formats, and check that each upload is readable and associated with the correct tender. Notify the portal administrator through the official contact channel if an account may be compromised.",
  },
  {
    title: "Applicable documents and interpretation",
    detail: "This page is a general portal guide, not an approved substitute for a tender document or legal advice. The current tender notice, schedules, corrigenda, and applicable Government of Maharashtra / Solapur Municipal Corporation rules prevail. Verify requirements for each tender before submission.",
  },
];

const REQUIRED_DOCUMENTS = [
  {
    name: "Tender-specific forms and acknowledgement",
    detail: "Completed tender schedules, declarations, and acknowledgement of the notice and applicable addenda.",
    applicability: "As specified in the tender",
  },
  {
    name: "Bidder constitution and registration proof",
    detail: "Incorporation, partnership, proprietorship, society, or other entity-registration documents establishing the bidder's legal status.",
    applicability: "As applicable to the bidder",
  },
  {
    name: "Authorized signatory proof",
    detail: "Power of attorney, board resolution, authorization letter, or equivalent evidence that the signatory may bind the bidder.",
    applicability: "If submitted by a representative",
  },
  {
    name: "PAN and tax registration",
    detail: "PAN and GST registration or exemption evidence, as required for the bidder and tender.",
    applicability: "As specified in the tender",
  },
  {
    name: "Eligibility and integrity declarations",
    detail: "Non-blacklisting, conflict-of-interest, integrity, and other declarations in the prescribed format.",
    applicability: "As specified in the tender",
  },
  {
    name: "Experience and performance evidence",
    detail: "Work orders, completion certificates, client references, or other evidence of similar work where qualification criteria require it.",
    applicability: "For experience-based criteria",
  },
  {
    name: "Financial qualification evidence",
    detail: "Audited statements, turnover certificates, solvency evidence, or other financial capacity documents where required.",
    applicability: "For financial criteria in the tender",
  },
  {
    name: "Tender fee and bid security evidence",
    detail: "Proof of tender-fee / EMD payment or the documents supporting a permitted exemption.",
    applicability: "Only when required by the tender",
  },
  {
    name: "Technical proposal and relevant licences",
    detail: "Technical response, specifications, method statements, product details, and required licences or registrations.",
    applicability: "As specified in the tender",
  },
  {
    name: "Price bid / BOQ",
    detail: "The completed price schedule in the prescribed format and submission section.",
    applicability: "As specified; keep separate where required",
  },
  {
    name: "Tender-specific permits and certificates",
    detail: "Any sector, safety, labour, environmental, quality, or other certificate expressly required for the work.",
    applicability: "Only where listed in the tender",
  },
];

export default function TermsPage() {
  return (
    <main className="mx-auto w-full max-w-5xl px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
      <PageHeader
        title="Terms & Conditions"
        description="General portal participation guidance for bidders using the Solapur Municipal Corporation tender portal."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button asChild variant="outline" size="sm">
              <Link href="/">Home</Link>
            </Button>
            <Button asChild variant="outline" size="sm">
              <Link href="/tenders">Browse tenders</Link>
            </Button>
          </div>
        }
      />

      <div className="mb-6 rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950">
        This is a general checklist, not a substitute for the official tender documents. Requirements vary by tender; the tender notice, schedules, corrigenda, and applicable rules take precedence. Confirm this wording with the Corporation's procurement/legal authority before treating it as an official policy.
      </div>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <h2 className="text-lg font-semibold">Portal terms</h2>
          <ol className="mt-4 space-y-4">
            {TERMS.map((term, index) => (
              <li key={term.title} className="flex gap-3">
                <span className="w-8 shrink-0 text-right font-semibold tabular-nums text-muted-foreground">{index + 1}.</span>
                <div>
                  <h3 className="font-medium">{term.title}</h3>
                  <p className="mt-1 text-sm leading-6 text-muted-foreground">{term.detail}</p>
                </div>
              </li>
            ))}
          </ol>
        </CardContent>
      </Card>

      <Card className="mt-6">
        <CardContent className="p-4 sm:p-6">
          <h2 className="text-lg font-semibold">Required document checklist</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Check the tender notice for the final document list, required format, validity period, and whether each item applies.
          </p>
          <div className="mt-4 overflow-x-auto">
            <table className="w-full min-w-[640px] border-collapse text-left text-sm">
              <thead>
                <tr className="border-b bg-muted/40">
                  <th scope="col" className="w-16 px-3 py-2 font-semibold">S. No.</th>
                  <th scope="col" className="px-3 py-2 font-semibold">Document</th>
                  <th scope="col" className="px-3 py-2 font-semibold">What to prepare</th>
                  <th scope="col" className="px-3 py-2 font-semibold">Applicability</th>
                </tr>
              </thead>
              <tbody>
                {REQUIRED_DOCUMENTS.map((document, index) => (
                  <tr key={document.name} className="border-b last:border-0">
                    <td className="px-3 py-3 align-top tabular-nums text-muted-foreground">{index + 1}</td>
                    <th scope="row" className="px-3 py-3 align-top font-medium">{document.name}</th>
                    <td className="px-3 py-3 align-top text-muted-foreground">{document.detail}</td>
                    <td className="px-3 py-3 align-top text-muted-foreground">{document.applicability}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </main>
  );
}
