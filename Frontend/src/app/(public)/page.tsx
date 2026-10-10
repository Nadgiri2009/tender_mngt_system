import { HeroSection } from "@/components/home/HeroSection";
import { StatsSection } from "@/components/home/StatsSection";
import { AboutSection } from "@/components/home/AboutSection";
import { LatestTendersSection } from "@/components/home/LatestTendersSection";
import { DepartmentsSection } from "@/components/home/DepartmentsSection";
import { AnnouncementsSection } from "@/components/home/AnnouncementsSection";
import { FAQSection } from "@/components/home/FAQSection";

export default function HomePage() {
  return (
    <main>
      <HeroSection />
      <StatsSection />
      <AboutSection />
      <LatestTendersSection />
      <DepartmentsSection />
      <AnnouncementsSection />
      <FAQSection />
    </main>
  );
}
