using System.ComponentModel.DataAnnotations;

namespace eShopLite.BasketService.Models;

public class BasketItem : IValidatableObject
{
    public string? Id { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal OldUnitPrice { get; set; }
    public int Quantity { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult>? results = null;

        if (Quantity < 1)
        {
            results = [ new("Invalid number of units", [ "Quantity" ]) ];
        }

        return results ?? Enumerable.Empty<ValidationResult>();
    }
}
