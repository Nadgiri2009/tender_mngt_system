using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Work;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Services
{
    public class WorkService : IWorkService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public WorkService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<int> CreateAgreementAsync(CreateAgreementDto dto, int currentUserId)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);

            var agreement = new Agreement
            {
                TenderId = dto.TenderId,
                ContractorId = dto.ContractorId,
                AgreementNo = dto.AgreementNo,
                AgreementDate = dto.AgreementDate,
                DocumentPath = dto.DocumentPath,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Agreements.AddAsync(agreement);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "CreateAgreement", nameof(Agreement), agreement.AgreementId);

            return agreement.AgreementId;
        }

        public async Task<int> CreateWorkOrderAsync(CreateWorkOrderDto dto, int currentUserId)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);

            var workOrder = new WorkOrder
            {
                TenderId = dto.TenderId,
                ContractorId = dto.ContractorId,
                OrderNo = dto.OrderNo,
                IssueDate = dto.IssueDate,
                Amount = dto.Amount,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.WorkOrders.AddAsync(workOrder);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "CreateWorkOrder", nameof(WorkOrder), workOrder.WorkOrderId);

            return workOrder.WorkOrderId;
        }

        public async Task UpdateProgressAsync(UpdateWorkProgressDto dto, int currentUserId)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);

            if (dto.ProgressPercentage < 0 || dto.ProgressPercentage > 100)
            {
                throw new BusinessRuleException("Progress percentage must be between 0 and 100.");
            }

            var execution = new WorkExecution
            {
                TenderId = dto.TenderId,
                ProgressPercentage = dto.ProgressPercentage,
                Remarks = dto.Remarks,
                UpdatedBy = currentUserId,
                UpdatedDate = DateTime.UtcNow
            };

            await _unitOfWork.WorkExecutions.AddAsync(execution);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "UpdateProgress", nameof(WorkExecution), execution.ExecutionId, dto.ProgressPercentage.ToString());
        }

        public async Task<int> CreateMeasurementBookEntryAsync(CreateMeasurementBookDto dto, int currentUserId)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);

            var mb = new MeasurementBook
            {
                TenderId = dto.TenderId,
                Description = dto.Description,
                Quantity = dto.Quantity,
                MeasurementDate = DateTime.UtcNow,
                RecordedBy = currentUserId,
                ApprovalStatus = "Pending"
            };

            await _unitOfWork.MeasurementBooks.AddAsync(mb);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "CreateMeasurementBook", nameof(MeasurementBook), mb.MBId);

            return mb.MBId;
        }

        public async Task<BillDto> CreateBillAsync(CreateBillDto dto)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);
            var contractor = await _unitOfWork.Contractors.GetByIdAsync(dto.ContractorId)
                ?? throw new NotFoundException(nameof(Contractor), dto.ContractorId);

            var bill = new Bill
            {
                TenderId = dto.TenderId,
                ContractorId = dto.ContractorId,
                BillNo = dto.BillNo,
                Amount = dto.Amount,
                Status = "Submitted",
                SubmittedDate = DateTime.UtcNow
            };

            await _unitOfWork.Bills.AddAsync(bill);
            await _unitOfWork.CompleteAsync();

            return ToDto(bill, contractor.Name);
        }

        public async Task<BillDto> ApproveBillAsync(int billId, int currentUserId)
        {
            var bill = await _unitOfWork.Bills.GetByIdAsync(billId) ?? throw new NotFoundException(nameof(Bill), billId);

            bill.Status = "Approved";
            bill.ApprovedBy = currentUserId;
            bill.ApprovedDate = DateTime.UtcNow;

            _unitOfWork.Bills.Update(bill);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "ApproveBill", nameof(Bill), bill.BillId);

            var contractor = await _unitOfWork.Contractors.GetByIdAsync(bill.ContractorId);
            return ToDto(bill, contractor?.Name ?? string.Empty);
        }

        public async Task<List<BillDto>> GetBillsAsync(int tenderId)
        {
            var bills = await _unitOfWork.Bills.FindAsync(b => b.TenderId == tenderId);
            var result = new List<BillDto>();
            foreach (var b in bills)
            {
                var contractor = await _unitOfWork.Contractors.GetByIdAsync(b.ContractorId);
                result.Add(ToDto(b, contractor?.Name ?? string.Empty));
            }
            return result;
        }

        public async Task ProcessPaymentAsync(ProcessPaymentDto dto, int currentUserId)
        {
            var bill = await _unitOfWork.Bills.GetByIdAsync(dto.BillId) ?? throw new NotFoundException(nameof(Bill), dto.BillId);

            if (bill.Status != "Approved")
            {
                throw new BusinessRuleException("Only an approved bill can be paid.");
            }

            var payment = new Payment
            {
                BillId = dto.BillId,
                Amount = dto.Amount,
                TransactionNo = dto.TransactionNo,
                PaymentDate = DateTime.UtcNow,
                ProcessedBy = currentUserId
            };

            await _unitOfWork.Payments.AddAsync(payment);

            bill.Status = "Paid";
            _unitOfWork.Bills.Update(bill);

            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "ProcessPayment", nameof(Payment), payment.PaymentId, dto.TransactionNo);
        }

        private static BillDto ToDto(Bill b, string contractorName) => new()
        {
            BillId = b.BillId,
            TenderId = b.TenderId,
            ContractorName = contractorName,
            BillNo = b.BillNo,
            Amount = b.Amount,
            Status = b.Status,
            SubmittedDate = b.SubmittedDate
        };
    }
}
