"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { FileCheck2, Gavel, Trophy } from "lucide-react";

import {
  fetchTechnicalBids,
  fetchFinancialBidsRanked,
  fetchMyContractorProfile,
  fetchContractors,
  submitTechnicalBid,
  evaluateTechnicalBid,
  submitFinancialBid,
  selectContractor,
  isBidStageOrLater,
  type TechnicalBidDto,
} from "@/lib/api/bids";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { formatCurrencyINR, formatDate } from "@/lib/utils";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
  DialogClose,
} from "@/components/ui/dialog";

interface BidsPanelProps {
  tenderId: number;
  currentStatus: string;
}

function bidStatusVariant(status: string): "default" | "success" | "destructive" | "secondary" {
  if (status === "Qualified" || status === "Selected") return "success";
  if (status === "Disqualified" || status === "Rejected") return "destructive";
  if (status === "Submitted") return "secondary";
  return "default";
}

export function BidsPanel({ tenderId, currentStatus }: BidsPanelProps) {
  const { isAuthenticated, user } = useAuth();
  const queryClient = useQueryClient();
  const isContractor = user?.role === "Contractor";
  const isAdmin = user?.role === "Admin";
  const canEvaluateTechnical = user?.role === "TenderSection" || isAdmin;
  const canSelectContractor = user?.role === "TenderCommittee" || isAdmin;

  const [evaluateTarget, setEvaluateTarget] = useState<TechnicalBidDto | null>(null);
  const [evaluateRemarks, setEvaluateRemarks] = useState("");
  const [techDialogOpen, setTechDialogOpen] = useState(false);
  const [finDialogOpen, setFinDialogOpen] = useState(false);
  const [techContractorId, setTechContractorId] = useState<string>("");
  const [techDocPath, setTechDocPath] = useState("");
  const [finAmount, setFinAmount] = useState("");
  const [selectRemarks, setSelectRemarks] = useState<Record<number, string>>({});

  const relevant = isBidStageOrLater(currentStatus);

  const { data: technicalBids, isLoading: loadingTechnical } = useQuery({
    queryKey: ["technical-bids", tenderId],
    queryFn: () => fetchTechnicalBids(tenderId),
    enabled: isAuthenticated && relevant,
  });

  const { data: financialBids, isLoading: loadingFinancial } = useQuery({
    queryKey: ["financial-bids", tenderId],
    queryFn: () => fetchFinancialBidsRanked(tenderId),
    enabled: isAuthenticated && relevant,
  });

  // Only fetched for Contractor users, to auto-resolve their own ContractorId.
  const { data: myProfile } = useQuery({
    queryKey: ["my-contractor-profile"],
    queryFn: fetchMyContractorProfile,
    enabled: isAuthenticated && isContractor,
  });

  // Only fetched for Admin, who may submit a bid on behalf of any contractor.
  const { data: contractorList } = useQuery({
    queryKey: ["contractors"],
    queryFn: fetchContractors,
    enabled: isAuthenticated && isAdmin && techDialogOpen,
  });

  const invalidateBids = () => {
    queryClient.invalidateQueries({ queryKey: ["technical-bids", tenderId] });
    queryClient.invalidateQueries({ queryKey: ["financial-bids", tenderId] });
  };

  const submitTechMutation = useMutation({
    mutationFn: (contractorId: number) => submitTechnicalBid(tenderId, contractorId, techDocPath || undefined),
    onSuccess: () => {
      toast.success("Technical bid submitted.");
      setTechDialogOpen(false);
      setTechDocPath("");
      setTechContractorId("");
      invalidateBids();
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to submit technical bid."),
  });

  const evaluateMutation = useMutation({
    mutationFn: (status: "Qualified" | "Disqualified") =>
      evaluateTechnicalBid(evaluateTarget!.technicalBidId, status, evaluateRemarks || undefined),
    onSuccess: () => {
      toast.success("Technical bid evaluated.");
      setEvaluateTarget(null);
      setEvaluateRemarks("");
      invalidateBids();
    },
    onError: (error: ApiError) => toast.error(error.message || "Evaluation failed."),
  });

  const submitFinMutation = useMutation({
    mutationFn: (contractorId: number) => submitFinancialBid(tenderId, contractorId, Number(finAmount)),
    onSuccess: () => {
      toast.success("Financial bid submitted.");
      setFinDialogOpen(false);
      setFinAmount("");
      invalidateBids();
    },
    onError: (error: ApiError) => toast.error(error.message || "Failed to submit financial bid."),
  });

  const selectMutation = useMutation({
    mutationFn: (contractorId: number) =>
      selectContractor(tenderId, contractorId, selectRemarks[contractorId] || undefined),
    onSuccess: () => {
      toast.success("Contractor selected.");
      invalidateBids();
    },
    onError: (error: ApiError) => toast.error(error.message || "Selection failed."),
  });

  if (!isAuthenticated || !relevant) {
    return null;
  }

  const myTechnicalBid = technicalBids?.find((b) => b.contractorId === myProfile?.contractorId);
  const canSubmitFinancial = isContractor && myTechnicalBid?.status === "Qualified";
  const alreadySubmittedFinancial = financialBids?.some((b) => b.contractorId === myProfile?.contractorId);
  const hasSelection = financialBids?.some((b) => b.status === "Selected");

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Gavel className="h-4 w-4" /> Bids
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Technical bids */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <h4 className="text-sm font-semibold text-foreground">Technical Bids</h4>
            {(isContractor || isAdmin) && (
              <Dialog open={techDialogOpen} onOpenChange={setTechDialogOpen}>
                <DialogTrigger asChild>
                  <Button size="sm" variant="outline">
                    <FileCheck2 className="mr-1 h-4 w-4" /> Submit Technical Bid
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Submit Technical Bid</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-3">
                    {isAdmin && (
                      <div className="space-y-1">
                        <label className="text-sm font-medium">Contractor</label>
                        <Select value={techContractorId} onValueChange={setTechContractorId}>
                          <SelectTrigger>
                            <SelectValue placeholder="Select contractor" />
                          </SelectTrigger>
                          <SelectContent>
                            {contractorList?.map((c) => (
                              <SelectItem key={c.contractorId} value={String(c.contractorId)}>
                                {c.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    )}
                    {isContractor && myProfile === null && (
                      <p className="text-sm text-destructive">
                        No contractor profile is linked to your account yet. Contact an administrator.
                      </p>
                    )}
                    {isContractor && myProfile && (
                      <p className="text-sm text-muted-foreground">
                        Submitting as <span className="font-medium text-foreground">{myProfile.name}</span>
                      </p>
                    )}
                    <div className="space-y-1">
                      <label className="text-sm font-medium">Document Path (optional)</label>
                      <Input
                        placeholder="e.g. /uploads/technical-bid.pdf"
                        value={techDocPath}
                        onChange={(e) => setTechDocPath(e.target.value)}
                      />
                    </div>
                  </div>
                  <DialogFooter>
                    <DialogClose asChild>
                      <Button variant="outline">Cancel</Button>
                    </DialogClose>
                    <Button
                      disabled={
                        submitTechMutation.isPending ||
                        (isAdmin && !techContractorId) ||
                        (isContractor && !myProfile)
                      }
                      onClick={() => {
                        const contractorId = isAdmin ? Number(techContractorId) : myProfile!.contractorId;
                        submitTechMutation.mutate(contractorId);
                      }}
                    >
                      {submitTechMutation.isPending ? "Submitting..." : "Submit"}
                    </Button>
                  </DialogFooter>
                </DialogContent>
              </Dialog>
            )}
          </div>

          {loadingTechnical && <Skeleton className="h-16 w-full" />}
          {!loadingTechnical && technicalBids?.length === 0 && (
            <p className="text-sm text-muted-foreground">No technical bids submitted yet.</p>
          )}
          {!loadingTechnical && technicalBids && technicalBids.length > 0 && (
            <div className="rounded-lg border border-border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Contractor</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Submitted</TableHead>
                    <TableHead>Remarks</TableHead>
                    {canEvaluateTechnical && <TableHead className="w-32" />}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {technicalBids.map((bid) => (
                    <TableRow key={bid.technicalBidId}>
                      <TableCell className="font-medium">{bid.contractorName}</TableCell>
                      <TableCell>
                        <Badge variant={bidStatusVariant(bid.status)}>{bid.status}</Badge>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{formatDate(bid.submittedDate)}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">{bid.remarks || "—"}</TableCell>
                      {canEvaluateTechnical && (
                        <TableCell>
                          {bid.status === "Submitted" && (
                            <Button size="sm" variant="outline" onClick={() => setEvaluateTarget(bid)}>
                              Evaluate
                            </Button>
                          )}
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </div>

        {/* Financial bids */}
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <h4 className="text-sm font-semibold text-foreground">Financial Bids</h4>
            {isContractor && canSubmitFinancial && !alreadySubmittedFinancial && (
              <Dialog open={finDialogOpen} onOpenChange={setFinDialogOpen}>
                <DialogTrigger asChild>
                  <Button size="sm" variant="outline">
                    Submit Financial Bid
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader>
                    <DialogTitle>Submit Financial Bid</DialogTitle>
                  </DialogHeader>
                  <div className="space-y-1">
                    <label className="text-sm font-medium">Bid Amount (₹)</label>
                    <Input
                      type="number"
                      step="0.01"
                      min="0.01"
                      value={finAmount}
                      onChange={(e) => setFinAmount(e.target.value)}
                    />
                  </div>
                  <DialogFooter>
                    <DialogClose asChild>
                      <Button variant="outline">Cancel</Button>
                    </DialogClose>
                    <Button
                      disabled={submitFinMutation.isPending || !finAmount || !myProfile}
                      onClick={() => submitFinMutation.mutate(myProfile!.contractorId)}
                    >
                      {submitFinMutation.isPending ? "Submitting..." : "Submit"}
                    </Button>
                  </DialogFooter>
                </DialogContent>
              </Dialog>
            )}
          </div>

          {isContractor && myTechnicalBid && myTechnicalBid.status !== "Qualified" && (
            <p className="text-xs text-muted-foreground">
              Financial bid submission opens once your technical bid is marked Qualified.
            </p>
          )}

          {loadingFinancial && <Skeleton className="h-16 w-full" />}
          {!loadingFinancial && financialBids?.length === 0 && (
            <p className="text-sm text-muted-foreground">No financial bids submitted yet.</p>
          )}
          {!loadingFinancial && financialBids && financialBids.length > 0 && (
            <div className="rounded-lg border border-border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Rank</TableHead>
                    <TableHead>Contractor</TableHead>
                    <TableHead className="text-right">Bid Amount</TableHead>
                    <TableHead>Status</TableHead>
                    {canSelectContractor && <TableHead className="w-40" />}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {financialBids.map((bid) => (
                    <TableRow key={bid.financialBidId}>
                      <TableCell>{bid.rank ? `L${bid.rank}` : "—"}</TableCell>
                      <TableCell className="font-medium">{bid.contractorName}</TableCell>
                      <TableCell className="text-right tabular-nums">
                        {formatCurrencyINR(bid.bidAmount)}
                      </TableCell>
                      <TableCell>
                        <Badge variant={bidStatusVariant(bid.status)}>{bid.status}</Badge>
                      </TableCell>
                      {canSelectContractor && (
                        <TableCell>
                          {!hasSelection && (
                            <Button
                              size="sm"
                              variant="default"
                              disabled={selectMutation.isPending}
                              onClick={() => selectMutation.mutate(bid.contractorId)}
                            >
                              <Trophy className="mr-1 h-4 w-4" /> Select
                            </Button>
                          )}
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </div>
      </CardContent>

      {/* Evaluate technical bid dialog */}
      <Dialog open={!!evaluateTarget} onOpenChange={(open) => !open && setEvaluateTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Evaluate Technical Bid — {evaluateTarget?.contractorName}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Textarea
              placeholder="Remarks (optional)"
              value={evaluateRemarks}
              onChange={(e) => setEvaluateRemarks(e.target.value)}
            />
          </div>
          <DialogFooter>
            <Button
              variant="destructive"
              disabled={evaluateMutation.isPending}
              onClick={() => evaluateMutation.mutate("Disqualified")}
            >
              Disqualify
            </Button>
            <Button disabled={evaluateMutation.isPending} onClick={() => evaluateMutation.mutate("Qualified")}>
              Qualify
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
