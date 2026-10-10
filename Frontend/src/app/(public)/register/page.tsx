"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { Building2 } from "lucide-react";

import { register as registerApi } from "@/lib/api/auth";
import { fetchRoles } from "@/lib/api/roles";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/contexts/AuthContext";
import { ROLE_NAMES } from "@/types/auth";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

const registerSchema = z.object({
  fullName: z.string().min(2, "Full name is required").max(150),
  email: z.string().email("Enter a valid email address"),
  mobile: z.string().min(10, "Enter a valid mobile number").max(15),
  password: z.string().min(6, "Password must be at least 6 characters"),
});

type RegisterFormValues = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const router = useRouter();
  const { setSession } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);

  // Public self-registration is only permitted for the Contractor/Vendor role
  // (enforced server-side in AuthService.RegisterAsync). We look up its real
  // RoleId rather than hardcoding a number, since seed data could renumber it.
  const { data: roles, isLoading: rolesLoading } = useQuery({
    queryKey: ["roles"],
    queryFn: fetchRoles,
  });
  const contractorRole = roles?.find((r) => r.roleName === ROLE_NAMES.Contractor);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) });

  const mutation = useMutation({
    mutationFn: (values: RegisterFormValues) => {
      if (!contractorRole) {
        throw new Error("Contractor role is not configured on the server yet.");
      }
      return registerApi({ ...values, roleId: contractorRole.roleId });
    },
    onSuccess: (auth) => {
      setSession(auth);
      toast.success(`Welcome, ${auth.fullName}. Your vendor account is ready.`);
      router.push("/tenders");
    },
    onError: (error: Error) => {
      setServerError(error instanceof ApiError ? error.message : error.message || "Registration failed.");
    },
  });

  return (
    <main className="flex min-h-[80vh] items-center justify-center px-4 py-16">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
            <Building2 className="h-6 w-6" />
          </div>
          <CardTitle className="mt-3">Vendor / Contractor Registration</CardTitle>
          <CardDescription>
            Register to bid on Solapur Municipal Corporation tenders. Government staff accounts are
            provisioned by an administrator, not through self-registration.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            className="space-y-4"
            onSubmit={handleSubmit((values) => {
              setServerError(null);
              mutation.mutate(values);
            })}
          >
            <div className="space-y-2">
              <Label htmlFor="fullName">Full Name</Label>
              <Input id="fullName" {...register("fullName")} />
              {errors.fullName && <p className="text-sm text-destructive">{errors.fullName.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" autoComplete="email" {...register("email")} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="mobile">Mobile Number</Label>
              <Input id="mobile" type="tel" {...register("mobile")} />
              {errors.mobile && <p className="text-sm text-destructive">{errors.mobile.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input id="password" type="password" autoComplete="new-password" {...register("password")} />
              {errors.password && <p className="text-sm text-destructive">{errors.password.message}</p>}
            </div>

            {serverError && <p className="text-sm text-destructive">{serverError}</p>}

            <Button
              type="submit"
              className="w-full"
              disabled={isSubmitting || mutation.isPending || rolesLoading || !contractorRole}
            >
              {mutation.isPending ? "Creating account..." : "Register as Contractor"}
            </Button>
          </form>

          <p className="mt-4 text-center text-sm text-muted-foreground">
            Already registered?{" "}
            <Link href="/login" className="font-medium text-primary hover:underline">
              Sign in
            </Link>
          </p>
        </CardContent>
      </Card>
    </main>
  );
}
