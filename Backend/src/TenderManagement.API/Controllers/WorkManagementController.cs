using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Work;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    public class WorkManagementController : ControllerBase
    {
        private readonly IWorkService _workService;

        public WorkManagementController(IWorkService workService)
        {
            _workService = workService;
        }

        /// <summary>POST /api/agreements - upload/record the signed contractor agreement</summary>
        [HttpPost("api/agreements")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Contractor},{RoleNames.Admin}")]
        public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementDto dto)
        {
            var id = await _workService.CreateAgreementAsync(dto, User.GetUserId());
            return Ok(new { agreementId = id });
        }

        /// <summary>POST /api/workorder/create</summary>
        [HttpPost("api/workorder/create")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> CreateWorkOrder([FromBody] CreateWorkOrderDto dto)
        {
            var id = await _workService.CreateWorkOrderAsync(dto, User.GetUserId());
            return Ok(new { workOrderId = id });
        }

        /// <summary>POST /api/work/update - contractor reports progress</summary>
        [HttpPost("api/work/update")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateWorkProgressDto dto)
        {
            await _workService.UpdateProgressAsync(dto, User.GetUserId());
            return Ok();
        }

        /// <summary>POST /api/mb/create - e-MB measurement book entry</summary>
        [HttpPost("api/mb/create")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Contractor},{RoleNames.Admin}")]
        public async Task<IActionResult> CreateMeasurementBook([FromBody] CreateMeasurementBookDto dto)
        {
            var id = await _workService.CreateMeasurementBookEntryAsync(dto, User.GetUserId());
            return Ok(new { mbId = id });
        }
    }

    [ApiController]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IWorkService _workService;

        public BillingController(IWorkService workService)
        {
            _workService = workService;
        }

        /// <summary>POST /api/bills/create</summary>
        [HttpPost("api/bills/create")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.Admin}")]
        public async Task<IActionResult> CreateBill([FromBody] CreateBillDto dto)
        {
            var result = await _workService.CreateBillAsync(dto);
            return Ok(result);
        }

        /// <summary>PUT /api/bills/{id}/approve</summary>
        [HttpPut("api/bills/{id:int}/approve")]
        [Authorize(Roles = $"{RoleNames.CAFOOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> ApproveBill(int id)
        {
            var result = await _workService.ApproveBillAsync(id, User.GetUserId());
            return Ok(result);
        }

        /// <summary>GET /api/bills/{tenderId}</summary>
        [HttpGet("api/bills/{tenderId:int}")]
        public async Task<IActionResult> GetBills(int tenderId)
        {
            var result = await _workService.GetBillsAsync(tenderId);
            return Ok(result);
        }

        /// <summary>POST /api/payment/process</summary>
        [HttpPost("api/payment/process")]
        [Authorize(Roles = $"{RoleNames.CAFOOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
        {
            await _workService.ProcessPaymentAsync(dto, User.GetUserId());
            return Ok();
        }
    }
}
