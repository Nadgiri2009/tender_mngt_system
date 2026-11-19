"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { Building2 } from "lucide-react";

import { register as registerApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { getAuthReturnUrl } from "@/lib/authRedirect";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

const registerSchema = z.object({
  fullName: z.string().trim().min(2, "Authorized representative name is required").max(150),
  companyName: z.string().trim().min(2, "Vendor or company name is required").max(150),
  address: z.string().trim().max(300, "Address must be 300 characters or fewer").optional(),
  email: z.string().email("Enter a valid email address"),
  mobile: z.string().regex(/^\d{10}$/, "Enter a 10-digit mobile number"),
  termsAccepted: z.literal(true, { errorMap: () => ({ message: "You must accept the Terms & Conditions" }) }),
});

type RegisterFormValues = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const router = useRouter();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) });

  const mutation = useMutation({
    mutationFn: (values: RegisterFormValues) => registerApi(values),
    onSuccess: (response) => {
      toast.success(response.message);
      const returnUrl = getAuthReturnUrl();
      router.replace(returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : "/login");
    },
    onError: (error: Error) => {
      setServerError(error instanceof ApiError ? error.message : error.message || "Registration failed.");
    },
  });

  return (
    <main className="flex min-h-[60vh] items-center justify-center px-4 py-8 sm:min-h-[70vh] sm:px-6 sm:py-12">
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
              <Label htmlFor="companyName">Vendor / Company Name <span aria-hidden="true">*</span></Label>
              <Input id="companyName" autoComplete="organization" {...register("companyName")} />
              {errors.companyName && <p className="text-sm text-destructive">{errors.companyName.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="fullName">Authorized Representative Name <span aria-hidden="true">*</span></Label>
              <Input id="fullName" {...register("fullName")} />
              {errors.fullName && <p className="text-sm text-destructive">{errors.fullName.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email Address <span aria-hidden="true">*</span></Label>
              <Input id="email" type="email" autoComplete="email" {...register("email")} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="mobile">Mobile Number <span aria-hidden="true">*</span></Label>
              <Input id="mobile" type="tel" {...register("mobile")} />
              {errors.mobile && <p className="text-sm text-destructive">{errors.mobile.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="address">Registered Address <span className="text-muted-foreground">(optional)</span></Label>
              <Input id="address" autoComplete="street-address" {...register("address")} />
              {errors.address && <p className="text-sm text-destructive">{errors.address.message}</p>}
            </div>
            <div className="space-y-2">
              <label htmlFor="termsAccepted" className="flex items-start gap-2 text-sm">
                <input id="termsAccepted" type="checkbox" className="mt-1" {...register("termsAccepted")} />
                <span>I accept the <Link href="/terms" target="_blank" className="font-medium text-primary underline">Terms &amp; Conditions</Link>. <span aria-hidden="true">*</span></span>
              </label>
              {errors.termsAccepted && <p className="text-sm text-destructive">{errors.termsAccepted.message}</p>}
            </div>

            {serverError && <p className="text-sm text-destructive">{serverError}</p>}

            <Button
              type="submit"
              className="w-full"
              disabled={isSubmitting || mutation.isPending}
            >
              {mutation.isPending ? "Creating account..." : "Register as Contractor"}
            </Button>
          </form>

          <p className="mt-4 text-center text-sm text-muted-foreground">
            Already registered?{" "}
            <Link
              href="/login"
              onClick={(event) => {
                const returnUrl = getAuthReturnUrl();
                if (!returnUrl) return;
                event.preventDefault();
                router.push(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
              }}
              className="font-medium text-primary hover:underline"
            >
              Sign in
            </Link>
          </p>
        </CardContent>
      </Card>
    </main>
  );
}
