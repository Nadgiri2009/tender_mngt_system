import Link from "next/link";
import { ArrowRight, FileSearch, UserPlus } from "lucide-react";
import { Button } from "@/components/ui/button";

export function HeroSection() {
  return (
    <section className="relative overflow-hidden bg-[hsl(var(--navy-950))]">
      <div
        className="absolute inset-0 opacity-10"
        style={{
          backgroundImage:
            "radial-gradient(circle at 20% 20%, white 1px, transparent 1px), radial-gradient(circle at 80% 60%, white 1px, transparent 1px)",
          backgroundSize: "48px 48px",
        }}
      />
      <div className="relative mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8 lg:py-28">
        <div className="max-w-2xl">
          <span className="inline-block rounded-full bg-[hsl(var(--saffron))]/15 px-3 py-1 text-xs font-semibold text-[hsl(var(--saffron))]">
            Government of Maharashtra
          </span>
          <h1 className="mt-4 text-3xl font-bold tracking-tight text-white sm:text-4xl lg:text-5xl">
            Transparent Digital Procurement for Solapur
          </h1>
          <p className="mt-4 text-base text-white/70 sm:text-lg">
            The official e-Tender portal of Solapur Municipal Corporation — publish, discover, and
            bid on municipal tenders through a single, auditable digital workflow.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
            <Button asChild size="lg" className="w-full sm:w-auto">
              <Link href="/tenders" className="inline-flex items-center justify-center">
                <FileSearch className="mr-2 h-5 w-5" /> View Tenders
              </Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="w-full border-white/30 bg-transparent text-white hover:bg-white/10 sm:w-auto"
            >
              <Link href="/register" className="inline-flex items-center justify-center">
                <UserPlus className="mr-2 h-5 w-5" /> Vendor Registration
                <ArrowRight className="ml-2 h-4 w-4" />
              </Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
