"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";

import { ApiError } from "@/lib/api/client";
import { fetchMyProfile, updateMyProfile } from "@/lib/api/auth";
import { useAuth } from "@/contexts/AuthContext";
import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";

const profileSchema = z.object({
  fullName: z.string().trim().min(2, "Enter your full name.").max(150),
  email: z.string().trim().email("Enter a valid email address.").max(150),
});

type ProfileFormValues = z.infer<typeof profileSchema>;

export default function ProfilePage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { user, isLoading: isAuthLoading, updateProfile } = useAuth();
  const { data: profile, isLoading, isError, error } = useQuery({
    queryKey: ["my-profile"],
    queryFn: fetchMyProfile,
    enabled: Boolean(user),
  });
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { fullName: "", email: "" },
  });

  useEffect(() => {
    if (!isAuthLoading && !user) {
      router.replace("/login?returnUrl=%2Fprofile");
    }
  }, [isAuthLoading, router, user]);

  useEffect(() => {
    if (profile) reset({ fullName: profile.fullName, email: profile.email });
  }, [profile, reset]);

  const updateMutation = useMutation({
    mutationFn: updateMyProfile,
    onSuccess: (savedProfile) => {
      const sessionUpdated = updateProfile({
        fullName: savedProfile.fullName,
        email: savedProfile.email,
      });
      if (!sessionUpdated) {
        toast.error("Your session expired. Sign in again to continue.");
        return;
      }
      queryClient.setQueryData(["my-profile"], savedProfile);
      reset({ fullName: savedProfile.fullName, email: savedProfile.email });
      toast.success("Profile saved.");
    },
    onError: (saveError: ApiError) => toast.error(saveError.message || "Could not save your profile."),
  });

  if (isAuthLoading || !user || isLoading) {
    return <div className="mx-auto max-w-2xl px-4 py-8"><Skeleton className="h-72 w-full" /></div>;
  }

  return (
    <main className="mx-auto w-full max-w-2xl px-4 py-8 sm:px-6">
      <PageHeader title="My Profile" description="Update the name and email saved to your user account." />
      {isError ? (
        <p role="alert" className="text-sm text-destructive">
          {error instanceof Error ? error.message : "Could not load your profile."}
        </p>
      ) : profile ? (
        <Card>
          <CardContent className="space-y-5 pt-6">
            <form
              onSubmit={handleSubmit((values) => updateMutation.mutate(values))}
              className="space-y-5"
            >
              <div className="space-y-2">
                <Label htmlFor="fullName">Full name</Label>
                <Input id="fullName" maxLength={150} {...register("fullName")} />
                {errors.fullName && <p className="text-sm text-destructive">{errors.fullName.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" maxLength={150} {...register("email")} />
                {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="mobile">Sign-in mobile</Label>
                <Input id="mobile" value={profile.mobile ?? "Not set"} readOnly />
                <p className="text-xs text-muted-foreground">
                  Mobile changes require administrator verification and cannot be changed here.
                </p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="role">Role</Label>
                <Input id="role" value={user.role} readOnly />
              </div>
              <Button type="submit" disabled={updateMutation.isPending}>
                {updateMutation.isPending ? "Saving..." : "Save profile"}
              </Button>
            </form>
          </CardContent>
        </Card>
      ) : null}
    </main>
  );
}
