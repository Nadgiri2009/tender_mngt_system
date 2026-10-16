using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Bid;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    public class BidService : IBidService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly IFileStorageService _fileStorageService;

        public BidService(
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _fileStorageService = fileStorageService;
        }

        public async Task<TechnicalBidDto> SubmitTechnicalBidAsync(SubmitTechnicalBidDto dto, int currentUserId, bool isAdmin)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);
            var contractor = await _unitOfWork.Contractors.GetByIdAsync(dto.ContractorId)
                ?? throw new NotFoundException(nameof(Contractor), dto.ContractorId);
            if (!isAdmin && contractor.UserId != currentUserId)
            {
                throw new NotFoundException(nameof(Contractor), dto.ContractorId);
            }

            var bid = new TechnicalBid
            {
                TenderId = dto.TenderId,
                ContractorId = dto.ContractorId,
                DocumentPath = dto.DocumentPath,
                Status = "Submitted",
                SubmittedDate = DateTime.UtcNow
            };

            await _unitOfWork.TechnicalBids.AddAsync(bid);
            await _unitOfWork.CompleteAsync();

            return ToDto(bid, contractor.Name);
        }

        public async Task<TenderApplicationDto> SubmitApplicationAsync(
            int tenderId,
            int userId,
            string termsVersion,
            IReadOnlyList<(string FileName, Stream Content)> files)
        {
            if (!string.Equals(termsVersion, TenderApplicationRules.TermsVersion, StringComparison.Ordinal))
            {
                throw new BusinessRuleException("The Terms & Conditions version has changed. Refresh the page and review the current terms.");
            }

            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);
            if (!TenderApplicationRules.CanApply(tender, DateTime.UtcNow))
            {
                throw new BusinessRuleException(
                    $"Applications cannot be submitted for this tender: {TenderApplicationRules.GetApplicationStatus(tender, DateTime.UtcNow)}.");
            }

            var contractors = await _unitOfWork.Contractors.FindAsync(contractor => contractor.UserId == userId);
            var contractor = contractors.FirstOrDefault();
            if (contractor is null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId)
                    ?? throw new NotFoundException(nameof(User), userId);
                contractor = new Contractor
                {
                    Name = user.FullName,
                    RegistrationNo = $"PORTAL-{user.UserId:D8}",
                    ContactNo = user.Mobile,
                    Email = user.Email,
                    UserId = user.UserId,
                    IsActive = true
                };
                await _unitOfWork.Contractors.AddAsync(contractor);
                await _unitOfWork.CompleteAsync();
            }

            if (!contractor.IsActive)
            {
                throw new BusinessRuleException("This contractor profile is inactive. Contact the portal administrator.");
            }

            var existingBids = await _unitOfWork.TechnicalBids.FindAsync(
                bid => bid.TenderId == tenderId && bid.ContractorId == contractor.ContractorId);
            if (existingBids.Any())
            {
                throw new BusinessRuleException("An application has already been submitted for this tender from this contractor account.");
            }

            var savedPaths = new List<string>();
            var savedFileNames = new List<string>();
            var submittedAt = DateTime.UtcNow;
            var bid = new TechnicalBid
            {
                TenderId = tenderId,
                ContractorId = contractor.ContractorId,
                Status = "Submitted",
                SubmittedDate = submittedAt,
                TermsAcceptedAt = submittedAt,
                TermsVersion = termsVersion
            };
            try
            {
                await _unitOfWork.TechnicalBids.AddAsync(bid);

                foreach (var file in files)
                {
                    var originalFileName = Path.GetFileName(file.FileName);
                    var (_, filePath) = await _fileStorageService.SaveFileAsync(
                        file.Content, originalFileName, Path.Combine("tender-applications", tenderId.ToString()));
                    savedPaths.Add(filePath);
                    savedFileNames.Add(originalFileName);

                    await _unitOfWork.TechnicalBidDocuments.AddAsync(new TechnicalBidDocument
                    {
                        TechnicalBid = bid,
                        FileName = originalFileName,
                        FilePath = filePath,
                        UploadedDate = submittedAt
                    });
                }

                if (savedPaths.Count > 0)
                {
                    bid.DocumentPath = savedPaths[0];
                    _unitOfWork.TechnicalBids.Update(bid);
                }

                await _unitOfWork.CompleteAsync();
            }
            catch
            {
                foreach (var filePath in savedPaths)
                {
                    await _fileStorageService.DeleteFileAsync(filePath);
                }
                throw;
            }

            await _auditService.LogAsync(userId, "SubmitTenderApplication", nameof(TechnicalBid), bid.TechnicalBidId, tender.TenderNumber);

            return new TenderApplicationDto
            {
                TechnicalBidId = bid.TechnicalBidId,
                TenderId = tenderId,
                ApplicationReference = $"TAPP-{bid.SubmittedDate:yyyy}-{bid.TechnicalBidId:D6}",
                Status = bid.Status,
                SubmittedDate = bid.SubmittedDate,
                Documents = savedFileNames
            };
        }

        public async Task<TechnicalBidDto> EvaluateTechnicalBidAsync(EvaluateTechnicalBidDto dto, int currentUserId)
        {
            var bid = await _unitOfWork.TechnicalBids.GetByIdAsync(dto.TechnicalBidId)
                ?? throw new NotFoundException(nameof(TechnicalBid), dto.TechnicalBidId);

            bid.Status = dto.Status;
            bid.Remarks = dto.Remarks;
            bid.EvaluatedBy = currentUserId;
            bid.EvaluatedDate = DateTime.UtcNow;

            _unitOfWork.TechnicalBids.Update(bid);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(currentUserId, "EvaluateTechnicalBid", nameof(TechnicalBid), bid.TechnicalBidId, dto.Status);

            var contractor = await _unitOfWork.Contractors.GetByIdAsync(bid.ContractorId);
            return ToDto(bid, contractor?.Name ?? string.Empty);
        }

        public async Task<List<TechnicalBidDto>> GetTechnicalBidsAsync(int tenderId)
        {
            var bids = await _unitOfWork.TechnicalBids.FindAsync(b => b.TenderId == tenderId);
            var result = new List<TechnicalBidDto>();
            foreach (var b in bids)
            {
                var contractor = await _unitOfWork.Contractors.GetByIdAsync(b.ContractorId);
                result.Add(ToDto(b, contractor?.Name ?? string.Empty));
            }
            return result;
        }

        public async Task<FinancialBidDto> SubmitFinancialBidAsync(SubmitFinancialBidDto dto)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId) ?? throw new NotFoundException(nameof(Tender), dto.TenderId);
            var contractor = await _unitOfWork.Contractors.GetByIdAsync(dto.ContractorId)
                ?? throw new NotFoundException(nameof(Contractor), dto.ContractorId);

            // Only contractors whose technical bid was qualified may submit a financial bid.
            var techBids = await _unitOfWork.TechnicalBids.FindAsync(b => b.TenderId == dto.TenderId && b.ContractorId == dto.ContractorId);
            var techBid = techBids.FirstOrDefault();
            if (techBid is null || techBid.Status != "Qualified")
            {
                throw new BusinessRuleException("Only contractors with a Qualified technical bid may submit a financial bid.");
            }

            var bid = new FinancialBid
            {
                TenderId = dto.TenderId,
                ContractorId = dto.ContractorId,
                BidAmount = dto.BidAmount,
                Status = "Submitted",
                SubmittedDate = DateTime.UtcNow
            };

            await _unitOfWork.FinancialBids.AddAsync(bid);
            await _unitOfWork.CompleteAsync();

            return new FinancialBidDto
            {
                FinancialBidId = bid.FinancialBidId,
                TenderId = bid.TenderId,
                ContractorId = bid.ContractorId,
                ContractorName = contractor.Name,
                BidAmount = bid.BidAmount,
                Rank = bid.Rank,
                Status = bid.Status
            };
        }

        public async Task<List<FinancialBidDto>> GetFinancialBidsRankedAsync(int tenderId)
        {
            var bids = (await _unitOfWork.FinancialBids.FindAsync(b => b.TenderId == tenderId))
                .OrderBy(b => b.BidAmount)
                .ToList();

            var result = new List<FinancialBidDto>();
            var rank = 1;
            foreach (var b in bids)
            {
                var contractor = await _unitOfWork.Contractors.GetByIdAsync(b.ContractorId);
                result.Add(new FinancialBidDto
                {
                    FinancialBidId = b.FinancialBidId,
                    TenderId = b.TenderId,
                    ContractorId = b.ContractorId,
                    ContractorName = contractor?.Name ?? string.Empty,
                    BidAmount = b.BidAmount,
                    Rank = rank,
                    Status = b.Status
                });
                rank++;
            }
            return result;
        }

        public async Task<FinancialBidDto> SelectContractorAsync(SelectContractorDto dto, int currentUserId)
        {
            var bids = await _unitOfWork.FinancialBids.FindAsync(b => b.TenderId == dto.TenderId);
            var selected = bids.FirstOrDefault(b => b.ContractorId == dto.ContractorId)
                ?? throw new NotFoundException(nameof(FinancialBid), dto.ContractorId);

            foreach (var b in bids)
            {
                b.Status = b.ContractorId == dto.ContractorId ? "Selected" : "Rejected";
                _unitOfWork.FinancialBids.Update(b);
            }

            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(currentUserId, "SelectContractor", nameof(FinancialBid), selected.FinancialBidId, dto.Remarks);

            var contractor = await _unitOfWork.Contractors.GetByIdAsync(selected.ContractorId);
            return new FinancialBidDto
            {
                FinancialBidId = selected.FinancialBidId,
                TenderId = selected.TenderId,
                ContractorId = selected.ContractorId,
                ContractorName = contractor?.Name ?? string.Empty,
                BidAmount = selected.BidAmount,
                Status = selected.Status
            };
        }

        private static TechnicalBidDto ToDto(TechnicalBid b, string contractorName) => new()
        {
            TechnicalBidId = b.TechnicalBidId,
            TenderId = b.TenderId,
            ContractorId = b.ContractorId,
            ContractorName = contractorName,
            Status = b.Status,
            Remarks = b.Remarks,
            SubmittedDate = b.SubmittedDate
        };
    }
}
