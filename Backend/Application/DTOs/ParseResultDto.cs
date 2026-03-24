using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.DTOs;

public class ParseResultDto
{
    public Brand Brand { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? StorageCapacity { get; set; }
    public string? Color { get; set; }
    public Condition Condition { get; set; }
    public decimal Price { get; set; }
    public bool IsValid => !string.IsNullOrEmpty(Model) && Price > 0;
}
