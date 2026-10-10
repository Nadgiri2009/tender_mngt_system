using TenderManagement.Application.DTOs.Work;

namespace TenderManagement.Application.Interfaces
{
    public interface IWorkService
    {
        Task<int> CreateAgreementAsync(CreateAgreementDto dto, int currentUserId);
        Task<int> CreateWorkOrderAsync(CreateWorkOrderDto dto, int currentUserId);
        Task UpdateProgressAsync(UpdateWorkProgressDto dto, int currentUserId);
        Task<int> CreateMeasurementBookEntryAsync(CreateMeasurementBookDto dto, int currentUserId);
        Task<BillDto> CreateBillAsync(CreateBillDto dto);
        Task<BillDto> ApproveBillAsync(int billId, int currentUserId);
        Task<List<BillDto>> GetBillsAsync(int tenderId);
        Task ProcessPaymentAsync(ProcessPaymentDto dto, int currentUserId);
    }
}
