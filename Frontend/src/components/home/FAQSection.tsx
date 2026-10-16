"use client";

import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const FAQS = [
  {
    q: "How can I register as a vendor or contractor?",
    a: "Choose Vendor Registration on the home page and provide your account and contractor profile details. Public registration creates a Contractor/Vendor account; staff accounts must be provisioned by an administrator.",
  },
  {
    q: "How do I log in to my registered vendor account?",
    a: "Use the Login page with the email address and password used at registration. If you cannot sign in, use the contact information shown in the portal footer for assistance.",
  },
  {
    q: "How can I view available tenders?",
    a: "Open View Tenders from the home page. The public list contains published tenders; internal drafts and tenders awaiting approval are not public.",
  },
  {
    q: "How do I apply for a published tender?",
    a: "Open the tender details and follow its application instructions. Application availability is limited to published tenders inside their configured submission window. If an Apply action is not available, consult the tender notice and use the portal contact details for assistance.",
  },
  {
    q: "What documents are required when submitting a tender application?",
    a: "Requirements vary by tender. Review the tender notice, attachments, and applicable terms for the authoritative document checklist; the general portal checklist is guidance only.",
  },
  {
    q: "Can I save a tender application as a draft?",
    a: "The current contractor application flow does not provide a save-as-draft option. Prepare the required information and documents before starting a submission.",
  },
  {
    q: "How can I check the status of my submitted application?",
    a: "Sign in and review the tender or application information available to your contractor account. If the application is not shown, contact the Corporation using the contact details in the portal footer.",
  },
  {
    q: "What happens if I miss the tender submission deadline?",
    a: "Applications are not accepted outside the tender's configured submission window. Check the tender notice for any official corrigendum or deadline change.",
  },
  {
    q: "How can I find tender fees, EMD, and security deposit requirements?",
    a: "Check the selected tender's official notice and attached documents. Amounts and conditions are tender-specific and are not inferred from this general FAQ.",
  },
  {
    q: "How will I know whether my tender application has been approved or rejected?",
    a: "Application and tender workflow statuses are shown where available after sign-in. Follow the official notices and tender-specific communication for decisions.",
  },
  {
    q: "How can a department user create and submit a tender for approval?",
    a: "An authorized Department Officer can create a tender from their dashboard. Creation saves it as a draft; use the tender's workflow action to submit it for the next configured stage. Only tenders that complete the required approvals and publication step appear publicly.",
  },
  {
    q: "Whom should I contact if I encounter a registration or submission issue?",
    a: "Use the Corporation contact information displayed in the portal footer. For a tender-specific question, refer to the official tender notice and its stated contact channel.",
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
              id={`faq-question-${i}`}
              type="button"
              className="flex w-full items-center justify-between px-5 py-4 text-left"
              onClick={() => setOpenIndex(openIndex === i ? null : i)}
              aria-expanded={openIndex === i}
              aria-controls={`faq-answer-${i}`}
            >
              <span className="text-sm font-medium text-foreground">{item.q}</span>
              <ChevronDown
                className={cn("h-4 w-4 text-muted-foreground transition-transform", openIndex === i && "rotate-180")}
              />
            </button>
            {openIndex === i && (
              <p id={`faq-answer-${i}`} aria-labelledby={`faq-question-${i}`} className="px-5 pb-4 text-sm text-muted-foreground">{item.a}</p>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
