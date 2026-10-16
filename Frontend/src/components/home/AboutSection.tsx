import { ShieldCheck, Workflow, Users2, ClipboardCheck } from "lucide-react";

const FEATURES = [
  { icon: ShieldCheck, title: "Transparent Procurement", desc: "Every tender follows a documented, auditable approval workflow." },
  { icon: Workflow, title: "Digital Workflow", desc: "From department requirement to payment, entirely online." },
  { icon: Users2, title: "Vendor Portal", desc: "Contractors register, bid, and track evaluation status digitally." },
  { icon: ClipboardCheck, title: "Complete Audit Trail", desc: "Every approval and status change is logged and traceable." },
];

export function AboutSection() {
  return (
    <section id="about" className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-3xl text-center">
        <h2 className="text-2xl font-bold text-foreground">About the Tender Portal</h2>
        <p className="mt-3 text-muted-foreground">
          This portal digitizes Solapur Municipal Corporation&apos;s public procurement process —
          from department requirement through technical sanction, committee approval, publication,
          bidding, evaluation, and final payment — replacing paper-based tendering with a single,
          transparent, auditable system.
        </p>
      </div>

      <div className="mt-10 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {FEATURES.map((f) => (
          <div key={f.title} className="rounded-lg border border-border p-5 text-center">
            <f.icon className="mx-auto h-8 w-8 text-primary" />
            <h3 className="mt-3 text-sm font-semibold text-foreground">{f.title}</h3>
            <p className="mt-1 text-xs text-muted-foreground">{f.desc}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
