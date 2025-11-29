using CrossPlatform.Web.Api.Data.Model;
using Microsoft.Azure.Cosmos;

namespace CrossPlatform.Web.Api.Data;

public class CosmosSeeder(CosmosClient client)
{
    private const string DatabaseName = "app";
    private const string ContainerName = "tasks";

    public async Task<IEnumerable<Geography>> EnsureSeededAndReadAllAsync()
    {
        var container = client.GetContainer(DatabaseName, ContainerName);
        var iterator = container.GetItemQueryIterator<Geography>("SELECT * FROM c");
        
        if (iterator.HasMoreResults)
        {
            var firstPage = await iterator.ReadNextAsync();
            if (firstPage.Count > 0)
            {
                return await ReadAllGeographiesAsync(container);
            }
        }

        await SeedGeographiesAsync(container);
        return await ReadAllGeographiesAsync(container);
    }

    private async Task SeedGeographiesAsync(Container container)
    {
        var geographies = GenerateGeographyItems();

        foreach (var geography in geographies)
        {
            await container.CreateItemAsync(geography, new PartitionKey(geography.Id));
        }
    }

    private async Task<IEnumerable<Geography>> ReadAllGeographiesAsync(Container container)
    {
        var iterator = container.GetItemQueryIterator<Geography>("SELECT * FROM c");
        List<Geography> allItems = [];

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            allItems.AddRange(response);
        }

        return allItems;
    }

    private static List<Geography> GenerateGeographyItems()
    {
        var now = DateTime.UtcNow;
        var geographies = new List<Geography>
        {
            new Geography
            {
                Id = "1",
                Name = "London",
                Description = "Capital city of England and the United Kingdom",
                CountryCode = "GB",
                Region = "Greater London",
                Longitude = -0.1276,
                Latitude = 51.5074,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "2",
                Name = "New York",
                Description = "Largest city in the United States",
                CountryCode = "US",
                Region = "New York",
                Longitude = -74.0060,
                Latitude = 40.7128,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "3",
                Name = "Tokyo",
                Description = "Capital city of Japan",
                CountryCode = "JP",
                Region = "Kanto",
                Longitude = 139.6503,
                Latitude = 35.6762,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "4",
                Name = "Paris",
                Description = "Capital city of France",
                CountryCode = "FR",
                Region = "Île-de-France",
                Longitude = 2.3522,
                Latitude = 48.8566,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "5",
                Name = "Sydney",
                Description = "Largest city in Australia",
                CountryCode = "AU",
                Region = "New South Wales",
                Longitude = 151.2093,
                Latitude = -33.8688,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "6",
                Name = "Berlin",
                Description = "Capital city of Germany",
                CountryCode = "DE",
                Region = "Berlin",
                Longitude = 13.4050,
                Latitude = 52.5200,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "7",
                Name = "Toronto",
                Description = "Largest city in Canada",
                CountryCode = "CA",
                Region = "Ontario",
                Longitude = -79.3832,
                Latitude = 43.6532,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "8",
                Name = "São Paulo",
                Description = "Largest city in Brazil",
                CountryCode = "BR",
                Region = "São Paulo",
                Longitude = -46.6333,
                Latitude = -23.5505,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "9",
                Name = "Dubai",
                Description = "Major city in the United Arab Emirates",
                CountryCode = "AE",
                Region = "Dubai",
                Longitude = 55.2708,
                Latitude = 25.2048,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            },
            new Geography
            {
                Id = "10",
                Name = "Singapore",
                Description = "City-state in Southeast Asia",
                CountryCode = "SG",
                Region = "Singapore",
                Longitude = 103.8198,
                Latitude = 1.3521,
                IsActive = true,
                DateCreated = now,
                LastUpdated = now,
                CreatedBy = "System",
                UpdatedBy = "System"
            }
        };

        return geographies;
    }
}

