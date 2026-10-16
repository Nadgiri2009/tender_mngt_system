"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { CheckCircle2, XCircle, Send, History, Clock3 } from "lucide-react";

import {
  submitTender,
  approveTender,
  rejectTender,
  fetchTenderHistory,
  getAvailableActions,
} from "@/lib/api/workflow";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { formatDate } from "@/lib/utils";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

const workflowOrder = [
  "Draft",
  "Requirement Submitted",
  "Estimate Prepared",
  "TS Approval Pending",
  "TS Approved",
  "GB Approval Pending",
  "GB Approved",
  "Administrative Approval Pending",
  "Administrative Approved",
  "Budget Approval Pending",
  "Budget Approved",
  "Tender Draft Created",
  "Tender Committee Approval",
  "Publication Approval Pending",
  "Tender Published",
  "Technical Bid Opening",
  "Technical Evaluation",
  "Technical Evaluation Approved",
  "Financial Bid Opening",
  "CAFO Rate Opinion",
  "Financial Evaluation",
  "MCA Opinion",
  "Tender Committee Recommendation",
  "Final Approval",
  "Agreement Completed",
  "Work Order Generated",
  "Work Started",
  "Measurement Book Updated",
  "Bill Submitted",
  "Bill Approved",
  "Payment Completed",
  "Rejected",
];

const workflowOwners: Record<string, { department: string; officer: string; stage: string }> = {
  Draft: { department: "Department", officer: "Department Officer", stage: "Requirement Submission" },
  "Requirement Submitted": { department: "Department", officer: "Department Officer", stage: "Requirement Submission" },
  "Estimate Prepared": { department: "Department", officer: "Department Officer", stage: "Estimate Preparation" },
  "TS Approval Pending": { department: "Technical Sanction", officer: "Technical Sanction Officer", stage: "Technical Sanction" },
  "TS Approved": { department: "Technical Sanction", officer: "Technical Sanction Officer", stage: "Technical Sanction" },
  "GB Approval Pending": { department: "GB", officer: "GB Officer", stage: "GB Approval" },
  "GB Approved": { department: "GB", officer: "GB Officer", stage: "GB Approval" },
  "Administrative Approval Pending": { department: "DPDC", officer: "DPDC Officer", stage: "Administrative Approval" },
  "Administrative Approved": { department: "DPDC", officer: "DPDC Officer", stage: "Administrative Approval" },
  "Budget Approval Pending": { department: "Finance", officer: "CAFO Officer", stage: "Budget Approval" },
  "Budget Approved": { department: "Finance", officer: "CAFO Officer", stage: "Budget Approval" },
  "Tender Draft Created": { department: "Tender Section", officer: "Department Officer", stage: "Tender Draft Preparation" },
  "Tender Committee Approval": { department: "Tender Committee", officer: "Tender Committee", stage: "Tender Committee Approval" },
  "Publication Approval Pending": { department: "Competent Authority", officer: "Competent Authority", stage: "Publication Approval" },
  "Tender Published": { department: "Tender Section", officer: "Tender Committee", stage: "Tender Publication" },
  "Technical Bid Opening": { department: "Tender Section", officer: "Tender Committee", stage: "Technical Bid Opening" },
  "Technical Evaluation": { department: "Tender Section", officer: "Tender Section", stage: "Technical Evaluation" },
  "Technical Evaluation Approved": { department: "Tender Section", officer: "Tender Section", stage: "Technical Evaluation" },
  "Financial Bid Opening": { department: "Tender Section", officer: "Tender Committee", stage: "Financial Bid Opening" },
  "CAFO Rate Opinion": { department: "CAFO", officer: "CAFO Officer", stage: "CAFO Rate Opinion" },
  "Financial Evaluation": { department: "Tender Section", officer: "Tender Committee", stage: "Financial Evaluation" },
  "MCA Opinion": { department: "MCA", officer: "MCA Officer", stage: "MCA Opinion" },
  "Tender Committee Recommendation": { department: "Tender Committee", officer: "Tender Committee", stage: "Tender Committee Recommendation" },
  "Final Approval": { department: "Competent Authority", officer: "Competent Authority", stage: "Final Authority Approval" },
  "Agreement Completed": { department: "Department", officer: "Department Officer", stage: "Agreement Execution" },
  "Work Order Generated": { department: "Department", officer: "Department Officer", stage: "Work Order" },
  "Work Started": { department: "Department", officer: "Department Officer", stage: "Work Execution" },
  "Measurement Book Updated": { department: "Department", officer: "Department Officer", stage: "Measurement Book" },
  "Bill Submitted": { department: "Department", officer: "Contractor", stage: "Bill Submission" },
  "Bill Approved": { department: "Finance", officer: "CAFO Officer", stage: "Bill Approval" },
  "Payment Completed": { department: "Finance", officer: "CAFO Officer", stage: "Payment Release" },
  Rejected: { department: "System", officer: "Rejected", stage: "Rejected" },
};

type ActionKind = "submit" | "approve" | "reject";

interface WorkflowPanelProps {
  tenderId: number;
  currentStatus: string;
  departmentName: string;
}

export function WorkflowPanel({ tenderId, currentStatus, departmentName }: WorkflowPanelProps) {
  const { isAuthenticated, user } = useAuth();
  const queryClient = useQueryClient();
  const [openAction, setOpenAction] = useState<ActionKind | null>(null);
  const [remarks, setRemarks] = useState("");

  const availableActions = getAvailableActions(currentStatus, user?.role);

  const { data: history, isLoading } = useQuery({
    queryKey: ["tender-history", tenderId],
    queryFn: () => fetchTenderHistory(tenderId),
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["tender-history", tenderId] });
    queryClient.invalidateQueries({ queryKey: ["tender", tenderId] });
    queryClient.invalidateQueries({ queryKey: ["tenders"] });
    queryClient.invalidateQueries({ queryKey: ["pending-approvals"] });
  };

  const submitMutation = useMutation({
    mutationFn: () => submitTender(tenderId, remarks || undefined),
    onSuccess: (result) => {
      toast.success(`Moved to ${result.newStatus}.`);
      setOpenAction(null);
      setRemarks("");
      invalidate();
    },
    onError: (error: ApiError) => toast.error(error.message || "Submit failed."),
  });

  const approveMutation = useMutation({
    mutationFn: () => approveTender(tenderId, currentStatus, remarks || undefined),
    onSuccess: (result) => {
      toast.success(`Approved. Moved to ${result.newStatus}.`);
      setOpenAction(null);
      setRemarks("");
      invalidate();
    },
    onError: (error: ApiError) => toast.error(error.message || "Approval failed."),
  });

  const rejectMutation = useMutation({
    mutationFn: () => rejectTender(tenderId, currentStatus, remarks || undefined),
    onSuccess: (result) => {
      toast.success(`Rejected. Moved to ${result.newStatus}.`);
      setOpenAction(null);
      setRemarks("");
      invalidate();
    },
    onError: (error: ApiError) => toast.error(error.message || "Reject failed."),
  });

  const steps = workflowOrder.map((status) => {
    const step = workflowOwners[status] ?? { department: "Workflow", officer: "—", stage: status };
    const currentIndex = workflowOrder.indexOf(currentStatus);
    const statusIndex = workflowOrder.indexOf(status);

    let state: "completed" | "current" | "upcoming" = "upcoming";
    if (status === currentStatus) state = "current";
    else if (statusIndex < currentIndex) state = "completed";

    return {
      status,
      department: step.department,
      officer: step.officer,
      stage: step.stage,
      state,
    };
  });

  const currentStep = steps.find((step) => step.state === "current") ?? steps[steps.length - 1] ?? {
    status: currentStatus || "Draft",
    department: departmentName || "Workflow",
    officer: "—",
    stage: currentStatus || "Draft",
    state: "upcoming" as const,
  };

  const currentIndex = workflowOrder.indexOf(currentStatus);
  const nextStep = workflowOrder.find((status, index) => index > currentIndex && status !== "Rejected") ?? null;
  const visibleSteps = workflowOrder.filter((status, index) => {
    if (status === "Rejected") return false;
    if (currentIndex === -1) return index <= 4;
    return index >= Math.max(0, currentIndex - 2) && index <= Math.min(workflowOrder.length - 1, currentIndex + 2);
  });

  if (!isAuthenticated) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Workflow</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">Sign in to view workflow actions available to your role.</p>
        </CardContent>
      </Card>
    );
  }

  const actionConfig: Record<ActionKind, { label: string; mutation: typeof submitMutation; icon: typeof Send }> = {
    submit: { label: "Submit / Advance", mutation: submitMutation, icon: Send },
    approve: { label: "Approve", mutation: approveMutation, icon: CheckCircle2 },
    reject: { label: "Reject", mutation: rejectMutation, icon: XCircle },
  };

  const visibleActions = Object.keys(actionConfig).filter((kind) => {
    const actionKind = kind as ActionKind;
    if (actionKind === "submit") return availableActions.canSubmit;
    if (actionKind === "approve") return availableActions.canApprove;
    if (actionKind === "reject") return availableActions.canReject;
    return false;
  }) as ActionKind[];

  const hasNoActions = visibleActions.length === 0;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <History className="h-4 w-4" /> Workflow
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="rounded-lg border bg-muted/20 p-3 sm:p-4">
          <div className="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-[10px] uppercase tracking-wide text-muted-foreground sm:text-xs">Current Pending Stage</p>
            <Badge variant={currentStatus === "Rejected" ? "destructive" : "warning"}>{currentStatus}</Badge>
          </div>
          <div className="grid gap-3 sm:grid-cols-3">
            <div>
              <p className="text-[10px] text-muted-foreground sm:text-xs">Department</p>
              <p className="text-sm font-medium text-foreground break-words">{departmentName || currentStep.department}</p>
            </div>
            <div>
              <p className="text-[10px] text-muted-foreground sm:text-xs">Officer</p>
              <p className="text-sm font-medium text-foreground break-words">{currentStep.officer}</p>
            </div>
            <div>
              <p className="text-[10px] text-muted-foreground sm:text-xs">Status</p>
              <p className="text-sm font-medium text-foreground">{hasNoActions ? "Pending" : "Awaiting action"}</p>
            </div>
          </div>
        </div>

        <div className="space-y-3">
          <div className="flex items-center justify-between gap-2">
            <p className="text-[10px] uppercase tracking-wide text-muted-foreground sm:text-xs">Progress</p>
            <p className="text-[11px] text-muted-foreground">
              {currentIndex >= 0 ? `${Math.round(((currentIndex + 1) / workflowOrder.length) * 100)}%` : "0%"}
            </p>
          </div>

          <div className="relative overflow-hidden px-1 py-3 sm:px-2">
            <div className="absolute left-6 right-6 top-[2.1rem] h-1 rounded-full bg-muted" />

            <div className="relative z-10 grid grid-cols-1 gap-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
              {visibleSteps.map((status) => {
                const stepIndex = workflowOrder.indexOf(status);
                const stepState =
                  stepIndex < currentIndex ? "completed" : stepIndex === currentIndex ? "current" : "upcoming";

                const isCompleted = stepState === "completed";
                const isCurrent = stepState === "current";
                const isUpcoming = stepState === "upcoming";

                return (
                  <div key={status} className="flex min-w-0 flex-col items-center text-center">
                    <div className="flex w-full items-center justify-center">
                      <div
                        className={[
                          "flex h-7 w-7 items-center justify-center rounded-full border-2 text-[10px] font-bold shadow-sm sm:h-8 sm:w-8 sm:text-xs",
                          isCompleted && "border-green-600 bg-green-600 text-white",
                          isCurrent && "border-green-600 bg-white text-green-700",
                          isUpcoming && "border-slate-300 bg-slate-300 text-slate-600",
                        ]
                          .filter(Boolean)
                          .join(" ")}
                      >
                        {isCompleted ? "✓" : isCurrent ? "•" : "○"}
                      </div>
                    </div>

                    <p className="mt-2 max-w-[90px] break-words text-center text-[9px] font-medium leading-tight text-foreground sm:text-[10px]">
                      {status}
                    </p>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="rounded-md border border-dashed bg-muted/20 p-3">
            <p className="text-[10px] uppercase tracking-wide text-muted-foreground sm:text-[11px]">Current pending step</p>
            <p className="mt-1 text-sm font-semibold text-foreground break-words">{currentStatus}</p>
            <p className="text-xs text-muted-foreground">
              {nextStep ? `Next: ${nextStep}` : "Final stage reached"}
            </p>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">
          {hasNoActions
            ? `No actions available in "${currentStatus}" status for your role.`
            : `Actions are validated server-side against your role and the tender's current stage.`}
        </p>

        {visibleActions.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {visibleActions.map((kind) => {
              const cfg = actionConfig[kind];
              return (
                <Dialog key={kind} open={openAction === kind} onOpenChange={(open) => setOpenAction(open ? kind : null)}>
                  <DialogTrigger asChild>
                    <Button
                      size="sm"
                      variant={kind === "reject" ? "destructive" : kind === "approve" ? "default" : "outline"}
                    >
                      <cfg.icon className="mr-1 h-4 w-4" /> {cfg.label}
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>{cfg.label}</DialogTitle>
                    </DialogHeader>
                    <div className="space-y-3">
                      <Textarea
                        placeholder="Remarks (optional)"
                        value={remarks}
                        onChange={(e) => setRemarks(e.target.value)}
                      />
                    </div>
                    <DialogFooter>
                      <DialogClose asChild>
                        <Button variant="outline">Cancel</Button>
                      </DialogClose>
                      <Button
                        onClick={() => cfg.mutation.mutate()}
                        disabled={cfg.mutation.isPending}
                        variant={kind === "reject" ? "destructive" : "default"}
                      >
                        {cfg.mutation.isPending ? "Working..." : `Confirm ${cfg.label}`}
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
              );
            })}
          </div>
        )}

        <div>
          <h4 className="mb-3 flex items-center gap-2 text-sm font-semibold text-foreground">
            <Clock3 className="h-4 w-4" /> Workflow History / Timeline
          </h4>
          {isLoading && <Skeleton className="h-24 w-full" />}
          {!isLoading && history?.length === 0 && (
            <p className="text-sm text-muted-foreground">No workflow actions recorded yet.</p>
          )}
          {!isLoading && history && history.length > 0 && (() => {
            const visibleHistory = [...history].slice(-5);

            return (
              <div className="pt-2">
                <div className="overflow-x-auto pb-1">
                  <div className="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
                    {visibleHistory.map((h, index) => {
                      const isLast = index === visibleHistory.length - 1;
                      const isFinished = h.status !== "Rejected";
                      const stageTitle = h.approvalStage || h.newStatus || "Workflow";

                      return (
                        <div key={h.approvalId} className="relative flex min-w-0 flex-col items-center text-center">
                          <div className="mb-2 text-[11px] font-medium text-foreground sm:text-sm">{formatDate(h.approvalDate)}</div>

                          <div className="relative flex w-full items-center justify-center">
                            {!isLast && (
                              <div className="absolute left-1/2 right-[-12%] top-1/2 h-1 -translate-y-1/2 rounded-full bg-slate-300" />
                            )}

                            <div
                              className={[
                                "relative z-10 flex h-7 w-7 items-center justify-center rounded-full border-2 shadow-sm sm:h-8 sm:w-8",
                                isFinished ? "border-emerald-600 bg-emerald-600 text-white" : "border-slate-300 bg-slate-300 text-slate-600",
                              ].join(" ")}
                            >
                              {isFinished ? "✓" : "×"}
                            </div>
                          </div>

                          <div className="mt-3 w-full">
                            <div className="inline-flex rounded-full bg-emerald-600 px-2 py-1 text-[9px] font-medium text-white sm:text-[11px]">
                              {h.status === "Rejected" ? "Rejected" : h.status}
                            </div>
                            <p className="mt-2 break-words text-[10px] font-medium text-foreground sm:text-[11px]">{stageTitle}</p>
                            <p className="mt-1 break-words text-[9px] text-muted-foreground sm:text-[10px]">{h.newStatus || "Updated"}</p>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            );
          })()}
        </div>
      </CardContent>
    </Card>
  );
}
