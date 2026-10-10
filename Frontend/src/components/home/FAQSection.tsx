"use client";

import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const FAQS = [
  {
    q: "Who can view tenders on this portal?",
    a: "Anyone. Published tenders, their documents, and closing dates are public — no login is required to search or view them.",
  },
  {
    q: "How do I submit a bid?",
    a: "Register as a Contractor/Vendor, log in, then submit your technical and financial bids from the tender's detail page before the submission deadline.",
  },
  {
    q: "Can I edit a tender after it's published?",
    a: "No. Once a department officer submits a tender past Draft status, it enters the approval workflow and can no longer be edited directly — only advanced through the defined workflow stages.",
  },
  {
    q: "How is EMD (Earnest Money Deposit) handled?",
    a: "EMD requirements are specified per tender in the tender documents. Refer to the specific tender's terms for the applicable amount and payment mode.",
  },
];

export function FAQSection() {
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  return (
    <section id="faq" className="mx-auto max-w-4xl px-4 py-16 sm:px-6 lg:px-8">
      <h2 className="text-center text-2xl font-bold text-foreground">Frequently Asked Questions</h2>
      <div className="mt-8 space-y-3">
        {FAQS.map((item, i) => (
          <div key={item.q} className="rounded-lg border border-border">
            <button
              className="flex w-full items-center justify-between px-5 py-4 text-left"
              onClick={() => setOpenIndex(openIndex === i ? null : i)}
              aria-expanded={openIndex === i}
            >
              <span className="text-sm font-medium text-foreground">{item.q}</span>
              <ChevronDown
                className={cn("h-4 w-4 text-muted-foreground transition-transform", openIndex === i && "rotate-180")}
              />
            </button>
            {openIndex === i && (
              <p className="px-5 pb-4 text-sm text-muted-foreground">{item.a}</p>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
