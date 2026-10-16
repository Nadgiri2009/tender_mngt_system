import Link from "next/link";
import Image from "next/image";
import { Mail, Phone, MapPin } from "lucide-react";

export function Footer() {
  return (
    <footer className="border-t border-border bg-[hsl(var(--navy-950))] text-white">
      <div className="mx-auto grid max-w-7xl grid-cols-1 gap-8 px-4 py-12 sm:grid-cols-2 sm:px-6 lg:grid-cols-4 lg:px-8">
        <div>
          <div className="flex items-center gap-2">
            <Image
              src="/smc.png"
              alt="Solapur Municipal Corporation seal"
              width={28}
              height={28}
              className="h-7 w-7 rounded-full bg-white object-contain"
            />
            <span className="font-semibold">SMC Tender Portal</span>
          </div>
          <p className="mt-3 text-sm text-white/70">
            The official e-Tender portal of Solapur Municipal Corporation, Maharashtra. Transparent,
            digital public procurement.
          </p>
        </div>

        <div>
          <h4 className="text-sm font-semibold uppercase tracking-wide text-white/90">Quick Links</h4>
          <ul className="mt-3 space-y-2 text-sm text-white/70">
            <li><Link href="/" className="hover:text-white">Home</Link></li>
            <li><Link href="/tenders" className="hover:text-white">Tender Notices</Link></li>
            <li><Link href="/register" className="hover:text-white">Vendor Registration</Link></li>
            <li><Link href="/login" className="hover:text-white">Login</Link></li>
          </ul>
        </div>

        <div>
          <h4 className="text-sm font-semibold uppercase tracking-wide text-white/90">Useful Links</h4>
          <ul className="mt-3 space-y-2 text-sm text-white/70">
            <li><Link href="/terms" className="hover:text-white">Terms &amp; Conditions</Link></li>
            <li><Link href="/#faq" className="hover:text-white">FAQ</Link></li>
          </ul>
        </div>

        <div id="contact">
          <h4 className="text-sm font-semibold uppercase tracking-wide text-white/90">Contact</h4>
          <ul className="mt-3 space-y-2 text-sm text-white/70">
            <li className="flex items-start gap-2">
              <MapPin className="mt-0.5 h-4 w-4 shrink-0" />
              Solapur Municipal Corporation, Solapur, Maharashtra 413001
            </li>
            <li className="flex items-center gap-2">
              <Phone className="h-4 w-4 shrink-0" /> 0217-2620401
            </li>
            <li className="flex items-center gap-2">
              <Mail className="h-4 w-4 shrink-0" /> tenders@solapurcorporation.gov.in
            </li>
          </ul>
        </div>
      </div>
      <div className="border-t border-white/10 py-4 text-center text-xs text-white/60">
        &copy; {new Date().getFullYear()} Solapur Municipal Corporation. All rights reserved.
      </div>
    </footer>
  );
}
