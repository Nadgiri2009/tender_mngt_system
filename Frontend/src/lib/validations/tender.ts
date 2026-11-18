import { z } from "zod";

// Mirrors [Required, StringLength(250, MinimumLength = 5)] on CreateTenderDto.Title / UpdateTenderDto.Title
const titleSchema = z
  .string()
  .trim()
  .min(5, "Title must be at least 5 characters")
  .max(250, "Title must be under 250 characters");

// Mirrors [StringLength(4000)]
const descriptionSchema = z
  .string()
  .max(4000, "Description must be under 4000 characters")
  .optional()
  .or(z.literal(""));

// Mirrors [Required, Range(0.01, double.MaxValue)]
const estimatedAmountSchema = z.coerce
  .number({ invalid_type_error: "Estimated amount is required" })
  .positive("Estimated amount must be greater than zero");

// Mirrors [Required, StringLength(50)]
const tenderTypeSchema = z
  .string()
  .min(1, "Tender type is required")
  .max(50, "Tender type must be under 50 characters");

const tenderCategorySchema = z
  .string()
  .min(1, "Tender category is required")
  .max(50, "Tender category must be under 50 characters");

const submissionDateSchema = z.string().optional().or(z.literal(""));

export const createTenderSchema = z.object({
  title: titleSchema,
  description: descriptionSchema,
  // Mirrors [Required, Range(1, int.MaxValue)] -- DepartmentId is nullable on the
  // backend so it can distinguish "not selected" from "0", but a valid tender
  // always needs a real department id > 0.
  departmentId: z.coerce
    .number({ invalid_type_error: "Department is required" })
    .int()
    .positive("A valid department must be selected"),
  estimatedAmount: estimatedAmountSchema,
  tenderType: tenderTypeSchema,
  tenderCategory: tenderCategorySchema,
  budgetInformation: z.string().max(4000, "Budget details must be under 4000 characters").optional().or(z.literal("")),
  technicalSanctionNumber: z.string().max(200).optional().or(z.literal("")),
  technicalSanctionAuthority: z.string().max(400).optional().or(z.literal("")),
  technicalSanctionDate: submissionDateSchema,
  preBidMeetingUtc: submissionDateSchema,
  preBidLocation: z.string().max(600).optional().or(z.literal("")),
  preBidDetails: z.string().max(4000).optional().or(z.literal("")),
  technicalBidOpeningScheduledUtc: submissionDateSchema,
  submissionStartDate: submissionDateSchema,
  submissionDeadline: submissionDateSchema,
}).superRefine((values, context) => {
  if (values.submissionStartDate && !values.submissionDeadline) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["submissionDeadline"],
      message: "A submission deadline is required when an application start date is set",
    });
  }

  if (
    values.submissionStartDate &&
    values.submissionDeadline &&
    (!Number.isFinite(new Date(values.submissionStartDate).getTime()) ||
      !Number.isFinite(new Date(values.submissionDeadline).getTime()) ||
      new Date(values.submissionDeadline) <= new Date(values.submissionStartDate))
  ) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["submissionDeadline"],
      message: "Submission deadline must be after the application start date",
    });
  }

  if (values.preBidMeetingUtc && values.submissionDeadline &&
    new Date(values.preBidMeetingUtc) >= new Date(values.submissionDeadline)) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["preBidMeetingUtc"],
      message: "The pre-bid meeting must be before the submission deadline",
    });
  }

  if (values.technicalBidOpeningScheduledUtc &&
    (!values.submissionDeadline ||
      new Date(values.technicalBidOpeningScheduledUtc) <= new Date(values.submissionDeadline))) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["technicalBidOpeningScheduledUtc"],
      message: "Technical bid opening must be after the submission deadline",
    });
  }
});

export const updateTenderSchema = z.object({
  title: titleSchema,
  description: descriptionSchema,
  estimatedAmount: estimatedAmountSchema,
  tenderType: tenderTypeSchema,
  tenderCategory: tenderCategorySchema,
  submissionDeadline: submissionDateSchema,
});

export type CreateTenderFormValues = z.infer<typeof createTenderSchema>;
export type UpdateTenderFormValues = z.infer<typeof updateTenderSchema>;

// Mirrors CreateBOQItemDto / UpdateBOQItemDto
export const boqItemSchema = z.object({
  itemName: z.string().min(1, "Item name is required").max(200),
  description: z.string().max(500).optional().or(z.literal("")),
  quantity: z.coerce.number().positive("Quantity must be greater than zero"),
  unit: z.string().min(1, "Unit is required").max(20),
  rate: z.coerce.number().positive("Rate must be greater than zero"),
});

export type BOQItemFormValues = z.infer<typeof boqItemSchema>;
