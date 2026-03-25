namespace WhatsAppParser.Domain.Enums;

public enum Condition
{
    Unknown = 0,
    New = 1,          // e.g. Lacrado
    Used = 2,         // e.g. Semi-novo
    Refurbished = 3,  // e.g. Vitrine / Swap
    Battery100 = 4,   // common for iPhones
    CPO = 5           // Certified Pre-Owned / Recondicionado
}
