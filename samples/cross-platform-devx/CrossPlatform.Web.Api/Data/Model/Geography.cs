namespace CrossPlatform.Web.Api.Data.Model;

public class Geography
{
    public int GeographyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public double Longitude { get; set; }

    public double Latitude { get; set; }

    public bool IsActive { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime LastUpdated { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}
