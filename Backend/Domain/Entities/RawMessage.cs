using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppParser.Domain.Entities;

public class RawMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? SupplierId { get; set; }

    [Required]
    public string OriginalText { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public bool ProcessedSuccessfully { get; set; }

    public string? ErrorMessage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
