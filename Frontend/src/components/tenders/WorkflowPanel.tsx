"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Clock3, History } from "lucide-react";

import {
  executeWorkflowAction,
  fetchAvailableWorkflowActions,
  fetchTenderHistory,
} from "@/lib/api/workflow";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { formatDate } from "@/lib/utils";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

interface WorkflowPanelProps {
  tenderId: number;
  currentStatus: string;
  departmentName: string;
}

function actionLabel(actionName: string): string {
  return actionName.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export function WorkflowPanel({ tenderId, currentStatus, departmentName }: WorkflowPanelProps) {
  const { isAuthenticated, user } = useAuth();
  const queryClient = useQueryClient();
  const [remarks, setRemarks] = useState("");

  const { data: actions = [], isLoading: actionsLoading } = useQuery({
    queryKey: ["workflow-actions", tenderId],
    queryFn: () => fetchAvailableWorkflowActions(tenderId),
    enabled: isAuthenticated && user?.accountType === "Internal",
  });

  const { data: history = [], isLoading: historyLoading } = useQuery({
    queryKey: ["tender-history", tenderId],
    queryFn: () => fetchTenderHistory(tenderId),
    enabled: isAuthenticated && user?.accountType === "Internal",
  });

  const actionMutation = useMutation({
    mutationFn: (actionName: string) => executeWorkflowAction(tenderId, actionName, remarks || undefined),
    onSuccess: (result) => {
      toast.success(`Moved to ${result.newStatus}.`);
      setRemarks("");
      queryClient.invalidateQueries({ queryKey: ["workflow-actions", tenderId] });
      queryClient.invalidateQueries({ queryKey: ["tender-history", tenderId] });
      queryClient.invalidateQueries({ queryKey: ["tender", tenderId] });
      queryClient.invalidateQueries({ queryKey: ["tenders"] });
      queryClient.invalidateQueries({ queryKey: ["pending-approvals"] });
    },
    onError: (error: ApiError) => toast.error(error.message || "Workflow action failed."),
  });

  if (!isAuthenticated) {
    return (
      <Card>
        <CardHeader><CardTitle className="text-base">Workflow</CardTitle></CardHeader>
        <CardContent><p className="text-sm text-muted-foreground">Sign in to view workflow actions available to your role.</p></CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base"><History className="h-4 w-4" /> Workflow</CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="grid gap-3 rounded-lg border bg-muted/20 p-4 sm:grid-cols-2">
          <div>
            <p className="text-xs text-muted-foreground">Current status</p>
            <p className="text-sm font-semibold">{currentStatus}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Department</p>
            <p className="text-sm font-semibold">{departmentName || "—"}</p>
          </div>
        </div>

        {actionsLoading ? <Skeleton className="h-10 w-full" /> : actions.length > 0 ? (
          <div className="space-y-3">
            <p className="text-sm font-medium">Actions configured for your role</p>
            <Textarea
              maxLength={1000}
              placeholder="Remarks (optional)"
              value={remarks}
              onChange={(event) => setRemarks(event.target.value)}
            />
            <div className="flex flex-wrap gap-2">
              {actions.map((action) => (
                <Button
                  key={`${action.actionName}-${action.stageName}`}
                  disabled={actionMutation.isPending}
                  onClick={() => actionMutation.mutate(action.actionName)}
                >
                  {actionLabel(action.actionName)}
                </Button>
              ))}
            </div>
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No workflow actions are currently configured for your role and this status.</p>
        )}

        <div>
          <h4 className="mb-3 flex items-center gap-2 text-sm font-semibold"><Clock3 className="h-4 w-4" />Workflow history</h4>
          {historyLoading ? <Skeleton className="h-20 w-full" /> : history.length === 0 ? (
            <p className="text-sm text-muted-foreground">No workflow actions recorded yet.</p>
          ) : (
            <ol className="space-y-3">
              {history.map((item) => (
                <li key={item.approvalId} className="border-l-2 border-primary/30 pl-3">
                  <p className="text-sm font-medium">{item.approvalStage}: {item.newStatus}</p>
                  <p className="text-xs text-muted-foreground">
                    {item.actionTakenBy || item.roleAtAction || "User"} · {formatDate(item.approvalDate)}
                  </p>
                  {item.remarks && <p className="mt-1 text-sm">{item.remarks}</p>}
                </li>
              ))}
            </ol>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
