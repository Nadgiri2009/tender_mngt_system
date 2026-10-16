"use client";

import { useQuery } from "@tanstack/react-query";
import { Megaphone } from "lucide-react";

import { fetchPublishedAnnouncements } from "@/lib/api/announcements";
import { formatDate } from "@/lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export function AnnouncementsSection() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["announcements", "published"],
    queryFn: fetchPublishedAnnouncements,
  });

  if (!isLoading && !isError && (!data || data.length === 0)) {
    return null;
  }

  return (
    <section id="announcements" className="bg-secondary/40 py-16">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h2 className="text-2xl font-bold text-foreground">Important Announcements</h2>
        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {isLoading &&
            Array.from({ length: 3 }).map((_, i) => (
              <Card key={i}>
                <CardContent className="pt-6">
                  <Skeleton className="h-5 w-3/4" />
                  <Skeleton className="mt-2 h-4 w-full" />
                </CardContent>
              </Card>
            ))}

          {isError && (
            <p className="text-sm text-destructive">Could not load announcements right now.</p>
          )}

          {data?.map((a) => (
            <Card key={a.announcementId}>
              <CardContent className="pt-6">
                <div className="flex items-start gap-3">
                  <Megaphone className="mt-0.5 h-5 w-5 shrink-0 text-accent" />
                  <div>
                    <p className="font-semibold text-foreground">{a.title}</p>
                    {a.body && <p className="mt-1 text-sm text-muted-foreground">{a.body}</p>}
                    <p className="mt-2 text-xs text-muted-foreground">{formatDate(a.publishDate ?? a.createdDate)}</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}
