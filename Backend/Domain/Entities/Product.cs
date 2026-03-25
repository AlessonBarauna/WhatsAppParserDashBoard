using System.ComponentModel.DataAnnotations;
using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Domain.Entities;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public Brand Brand { get; set; }

    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? StorageCapacity { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public Condition Condition { get; set; }

    [MaxLength(10)]
    public string? OriginFlag { get; set; } // emoji flag e.g. "🇺🇸", "🇧🇷"

    [Required]
    [MaxLength(150)]
    public string NormalizedName { get; set; } = string.Empty; // e.g. "APPLE IPHONE 13 PRO MAX 256GB"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
