namespace eShopLite.CatalogDb;

public class CatalogItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public required string PictureFileName { get; set; }
    public string? PictureUri { get; set; }

    public int CatalogTypeId { get; set; }
    public required CatalogType CatalogType { get; set; }

    public int CatalogBrandId { get; set; }
    public required CatalogBrand CatalogBrand { get; set; }
    public int AvailableStock { get; set; }
    public int RestockThreshold { get; set; }
    public int MaxStockThreshold { get; set; }
    public bool OnReorder { get; set; }
}
