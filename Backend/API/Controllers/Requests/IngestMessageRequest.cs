using System.ComponentModel.DataAnnotations;

namespace WhatsAppParser.API.Controllers.Requests;

public class IngestMessageRequest
{
    [Required]
    public string RawText { get; set; } = string.Empty;

    public string? SupplierName { get; set; }
    
    public string? SupplierPhoneNumber { get; set; }
}
