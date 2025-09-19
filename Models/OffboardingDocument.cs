using System.ComponentModel.DataAnnotations;

namespace OffboardingChecklist.Models
{
    public class OffboardingDocument
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty; // Exit Letter, Return Form, etc.

        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public string UploadedBy { get; set; } = string.Empty;

        public int OffboardingProcessId { get; set; }
        public OffboardingProcess OffboardingProcess { get; set; } = null!;

        public bool IsRequired { get; set; }
        public bool IsCompleted { get; set; }

        public string? Description { get; set; }
    }

    public enum DocumentType
    {
        ExitInterview,
        AssetReturnForm,
        FinalPayslip,
        ClearanceCertificate,
        HandoverDocument,
        AccessCardReturn,
        NonDisclosureAgreement,
        Other
    }
}