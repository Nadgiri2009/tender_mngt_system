# Officer SOP (PDF-Aligned) - Tender Lifecycle

Use this single page as the operational checklist for all officer logins from tender creation to final payment.

| Seq | Current Status | Who Logs In | Button/Action | Next Status |
|---|---|---|---|---|
| 1 | Draft | DepartmentOfficer | Submit / Advance | Requirement Submitted |
| 2 | Requirement Submitted | DepartmentOfficer | Submit / Advance | Estimate Prepared |
| 3 | Estimate Prepared | DepartmentOfficer | Submit / Advance | TS Approval Pending |
| 4 | TS Approval Pending | TechnicalSanctionOfficer | Approve | TS Approved |
| 5 | TS Approved | DepartmentOfficer | Submit / Advance | GB Approval Pending |
| 6 | GB Approval Pending | GBOfficer | Approve | GB Approved |
| 7 | GB Approved | DepartmentOfficer | Submit / Advance | Administrative Approval Pending |
| 8 | Administrative Approval Pending | DPDCOfficer | Approve | Administrative Approved |
| 9 | Administrative Approved | DepartmentOfficer | Submit / Advance | Budget Approval Pending |
| 10 | Budget Approval Pending | CAFOOfficer | Approve | Budget Approved |
| 11 | Budget Approved | DepartmentOfficer | Submit / Advance | Tender Draft Created |
| 12 | Tender Draft Created | DepartmentOfficer | Submit / Advance | Tender Committee Approval |
| 13 | Tender Committee Approval | TenderCommittee | Approve | Publication Approval Pending |
| 14 | Publication Approval Pending | CompetentAuthority | Approve | Tender Published |
| 15 | Tender Published | TenderCommittee or DepartmentOfficer | Submit / Advance | Technical Bid Opening |
| 16 | Technical Bid Opening | TenderCommittee | Submit / Advance | Technical Evaluation |
| 17 | Technical Evaluation | TenderSection | Approve | Technical Evaluation Approved |
| 18 | Technical Evaluation Approved | TenderCommittee | Approve | Financial Bid Opening |
| 19 | Financial Bid Opening | TenderCommittee | Submit / Advance | CAFO Rate Opinion |
| 20 | CAFO Rate Opinion | CAFOOfficer | Approve | Financial Evaluation |
| 21 | Financial Evaluation | TenderCommittee | Submit / Advance | MCA Opinion |
| 22 | MCA Opinion | MCAOfficer | Approve | Tender Committee Recommendation |
| 23 | Tender Committee Recommendation | TenderCommittee | Approve | Final Approval |
| 24 | Final Approval | CompetentAuthority | Approve | Agreement Completed |
| 25 | Agreement Completed | DepartmentOfficer or Contractor | Submit / Advance | Work Order Generated |
| 26 | Work Order Generated | DepartmentOfficer or Contractor | Submit / Advance | Work Started |
| 27 | Work Started | DepartmentOfficer or Contractor | Submit / Advance | Measurement Book Updated |
| 28 | Measurement Book Updated | Contractor | Submit / Advance | Bill Submitted |
| 29 | Bill Submitted | CAFOOfficer | Approve | Bill Approved |
| 30 | Bill Approved | CAFOOfficer or Admin | Submit / Advance | Payment Completed |

## Operational Notes

- Approve/Reject buttons are visible only for the mapped role at that status.
- Reject from an approval stage moves tender to Rejected and requires department-led resubmission flow.
- For existing databases, run Database/05_AlignTechnicalEvaluationWithPdf.sql once to align Technical Evaluation ownership and stage mapping with this SOP.

## Quick Test Accounts (from DatabaseSetup)

- DepartmentOfficer: dept.officer@smc.gov.in / Password@123
- TechnicalSanctionOfficer: ts.officer@smc.gov.in / Password@123
- TenderSection: tender.section@smc.gov.in / Password@123
- GBOfficer: gb.officer@smc.gov.in / Password@123
- DPDCOfficer: dpdc.officer@smc.gov.in / Password@123
- CAFOOfficer: cafo.officer@smc.gov.in / Password@123
- MCAOfficer: mca.officer@smc.gov.in / Password@123
- TenderCommittee: tender.committee@smc.gov.in / Password@123
- CompetentAuthority: competent.authority@smc.gov.in / Password@123
- Contractor: contractor@smc.gov.in / Password@123
