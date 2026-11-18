"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";

import { Button } from "@/components/ui/button";

interface PaginationControlsProps {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (pageNumber: number) => void;
}

function getVisiblePages(pageNumber: number, totalPages: number): Array<number | "ellipsis"> {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  const pages = new Set([1, totalPages, pageNumber - 1, pageNumber, pageNumber + 1]);
  const ordered = [...pages].filter((page) => page >= 1 && page <= totalPages).sort((a, b) => a - b);
  const visible: Array<number | "ellipsis"> = [];

  ordered.forEach((page, index) => {
    const previousPage = ordered[index - 1];
    if (previousPage !== undefined && page - previousPage > 1) visible.push("ellipsis");
    visible.push(page);
  });
  return visible;
}

export function PaginationControls({
  pageNumber,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
}: PaginationControlsProps) {
  if (totalCount === 0) return null;

  const firstItem = (pageNumber - 1) * pageSize + 1;
  const lastItem = Math.min(pageNumber * pageSize, totalCount);

  return (
    <div className="flex flex-col gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
      <p>Showing {firstItem}–{lastItem} of {totalCount}</p>
      <nav aria-label="Pagination" className="flex flex-wrap items-center gap-1">
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="Previous page"
          disabled={pageNumber <= 1}
          onClick={() => onPageChange(pageNumber - 1)}
        >
          <ChevronLeft className="h-4 w-4" />
          <span className="sr-only">Previous</span>
        </Button>
        {getVisiblePages(pageNumber, totalPages).map((page, index) => page === "ellipsis" ? (
          <span key={`ellipsis-${index}`} aria-hidden="true" className="px-2">…</span>
        ) : (
          <Button
            key={page}
            type="button"
            variant={page === pageNumber ? "default" : "outline"}
            size="sm"
            aria-label={`Page ${page}`}
            aria-current={page === pageNumber ? "page" : undefined}
            onClick={() => onPageChange(page)}
          >
            {page}
          </Button>
        ))}
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label="Next page"
          disabled={pageNumber >= totalPages}
          onClick={() => onPageChange(pageNumber + 1)}
        >
          <span className="sr-only">Next</span>
          <ChevronRight className="h-4 w-4" />
        </Button>
      </nav>
    </div>
  );
}
