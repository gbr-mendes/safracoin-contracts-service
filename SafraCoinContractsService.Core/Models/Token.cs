namespace SafraCoinContractsService.Core.Models;

public class Token
{
    public required Guid Id { get; set; } = Guid.NewGuid();
    public required Guid CropId { get; set; }
    public required uint Quantity { get; set; }
    public decimal InitialPrice { get; set; }
}
