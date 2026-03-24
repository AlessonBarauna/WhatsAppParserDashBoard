using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WhatsAppParser.Domain.Entities;

public class PriceHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProductId { get; set; }

    public Guid? SupplierId { get; set; }

    public Guid? RawMessageId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public DateTime DateLogged { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [ForeignKey(nameof(RawMessageId))]
    public RawMessage? RawMessage { get; set; }
}
