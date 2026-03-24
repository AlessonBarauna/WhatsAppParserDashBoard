using System.ComponentModel.DataAnnotations;

namespace WhatsAppParser.Domain.Entities;

public class Supplier
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public int ReliabilityScore { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RawMessage> RawMessages { get; set; } = new List<RawMessage>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
