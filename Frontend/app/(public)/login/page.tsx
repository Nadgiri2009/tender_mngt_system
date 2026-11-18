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

import { login as loginApi, requestLoginOtp } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";
import { getAuthReturnUrl } from "@/lib/authRedirect";
import { useAuth } from "@/contexts/AuthContext";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

const mobileSchema = z.object({
  mobile: z.string().regex(/^\d{10}$/, "Enter a 10-digit mobile number"),
});

const otpSchema = z.object({
  otp: z.string().regex(/^\d{6}$/, "Enter the 6-digit code"),
});

type MobileFormValues = z.infer<typeof mobileSchema>;
type OtpFormValues = z.infer<typeof otpSchema>;

export default function LoginPage() {
  const router = useRouter();
  const { setSession } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [activeMobile, setActiveMobile] = useState("");
  const [developmentOtp, setDevelopmentOtp] = useState<string | null>(null);
  const [step, setStep] = useState<"mobile" | "otp">("mobile");

  const mobileForm = useForm<MobileFormValues>({
    resolver: zodResolver(mobileSchema),
    defaultValues: { mobile: "" },
  });
  const otpForm = useForm<OtpFormValues>({
    resolver: zodResolver(otpSchema),
    defaultValues: { otp: "" },
  });

  const requestOtpMutation = useMutation({
    mutationFn: requestLoginOtp,
    onSuccess: (response, input) => {
      setActiveMobile(input.mobile);
      setDevelopmentOtp(response.developmentOtp ?? null);
      setStep("otp");
      otpForm.reset();
      setServerError(null);
      toast.success(response.message);
    },
    onError: (error: ApiError) => {
      setServerError(error.message || "Could not request a sign-in code.");
    },
  });

  const loginMutation = useMutation({
    mutationFn: loginApi,
    onSuccess: (auth) => {
      if (!setSession(auth)) {
        setServerError("The sign-in response was invalid or expired. Please request a new code.");
        return;
      }
      toast.success(`Welcome back, ${auth.fullName}.`);
      router.replace(getAuthReturnUrl() ?? (auth.role === "Junior Engineer (JE)" ? "/admin" : "/dashboard"));
    },
    onError: (error: ApiError) => {
      setServerError(error.message || "Login failed.");
    },
  });

  const requestCode = mobileForm.handleSubmit((values) => {
    setServerError(null);
    setDevelopmentOtp(null);
    requestOtpMutation.mutate(values);
  });

  const verifyCode = otpForm.handleSubmit((values) => {
    setServerError(null);
    loginMutation.mutate({ mobile: activeMobile, otp: values.otp });
  });

  return (
    <main className="flex min-h-[60vh] items-center justify-center px-4 py-8 sm:min-h-[70vh] sm:px-6 sm:py-12">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
            <Building2 className="h-6 w-6" />
          </div>
          <CardTitle className="mt-3">Sign in</CardTitle>
          <CardDescription>Solapur Municipal Corporation Tender Portal</CardDescription>
        </CardHeader>
        <CardContent>
          {step === "mobile" ? (
            <form className="space-y-4" onSubmit={requestCode}>
              <div className="space-y-2">
                <Label htmlFor="mobile">Mobile number</Label>
                <Input
                  id="mobile"
                  type="tel"
                  autoComplete="tel-national"
                  inputMode="numeric"
                  maxLength={10}
                  pattern="[0-9]{10}"
                  placeholder="10-digit mobile number"
                  {...mobileForm.register("mobile", {
                    onChange: (event) => {
                      event.target.value = event.target.value.replace(/\D/g, "").slice(0, 10);
                    },
                  })}
                />
                {mobileForm.formState.errors.mobile && (
                  <p className="text-sm text-destructive">{mobileForm.formState.errors.mobile.message}</p>
                )}
              </div>

              {serverError && <p className="text-sm text-destructive">{serverError}</p>}

              <Button type="submit" className="w-full" disabled={requestOtpMutation.isPending}>
                {requestOtpMutation.isPending ? "Requesting code..." : "Send sign-in code"}
              </Button>
            </form>
          ) : (
            <form className="space-y-4" onSubmit={verifyCode}>
              <div className="space-y-2">
                <Label htmlFor="active-mobile">Mobile number</Label>
                <div className="flex items-center justify-between gap-3">
                  <p id="active-mobile" className="text-sm text-muted-foreground">{activeMobile}</p>
                  <button
                    type="button"
                    className="text-sm font-medium text-primary hover:underline"
                    onClick={() => {
                      setServerError(null);
                      setDevelopmentOtp(null);
                      setStep("mobile");
                      otpForm.reset();
                    }}
                  >
                    Change number
                  </button>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="otp">6-digit sign-in code</Label>
                <Input
                  id="otp"
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  maxLength={6}
                  {...otpForm.register("otp")}
                />
                {otpForm.formState.errors.otp && (
                  <p className="text-sm text-destructive">{otpForm.formState.errors.otp.message}</p>
                )}
              </div>

              <p className="text-sm text-muted-foreground">
                If this number is registered, use the code sent to it.
              </p>
              {developmentOtp && (
                <p role="status" className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950">
                  Development OTP: <strong className="font-mono">{developmentOtp}</strong>
                  <span className="block text-xs">Visible only for local development.</span>
                </p>
              )}
              {serverError && <p className="text-sm text-destructive">{serverError}</p>}

              <Button type="submit" className="w-full" disabled={loginMutation.isPending}>
                {loginMutation.isPending ? "Verifying..." : "Verify and sign in"}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="w-full"
                disabled={requestOtpMutation.isPending}
                onClick={() => {
                  setServerError(null);
                  setDevelopmentOtp(null);
                  requestOtpMutation.mutate({ mobile: activeMobile });
                }}
              >
                {requestOtpMutation.isPending ? "Requesting code..." : "Resend code"}
              </Button>
            </form>
          )}

          <p className="mt-4 text-center text-sm text-muted-foreground">
            New vendor?{" "}
            <Link
              href="/register"
              onClick={(event) => {
                const returnUrl = getAuthReturnUrl();
                if (!returnUrl) return;
                event.preventDefault();
                router.push(`/register?returnUrl=${encodeURIComponent(returnUrl)}`);
              }}
              className="font-medium text-primary hover:underline"
            >
              Register as Contractor
            </Link>
          </p>
        </CardContent>
      </Card>
    </main>
  );
}
