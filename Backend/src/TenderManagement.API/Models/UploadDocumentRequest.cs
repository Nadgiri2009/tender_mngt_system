using System.ComponentModel.DataAnnotations;

namespace TenderManagement.API.Models
{
    /// <summary>
    /// Bound as a single [FromForm] model for multipart/form-data uploads. Swashbuckle/Swagger can't
    /// generate an OpenAPI schema for an action that mixes several individual [FromForm] primitive
    /// parameters with an IFormFile on the same action — binding everything through one model avoids
    /// that entirely, and is also just a cleaner action signature.
    ///
    /// This lives in the API project (not Application/DTOs) deliberately: IFormFile is an ASP.NET
    /// Core framework type, and the Application layer is a plain class library that shouldn't take a
    /// dependency on framework-specific HTTP types (that's what the Clean Architecture boundary in
    /// this solution is for). The controller still hands the service layer a plain Stream + filename.
    /// </summary>
    public class UploadDocumentRequest
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, StringLength(80)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
