# Database migrations with Entity Framework Core sample app

This sample demonstrates how to use Entity Framework Core's [migrations feature](https://learn.microsoft.com/ef/core/managing-schemas/migrations) with Aspire.

The sample has three important projects:

- `DatabaseMigrations.ApiService` - A web app that uses the database.
- `DatabaseMigrations.MigrationService` - A background worker app that applies migrations when it starts up.
- `DatabaseMigrations.ApiModel` - The EF Core context and entity types. This project is used by both the API and migration service.

`DatabaseMigrations.ApiService` and `DatabaseMigrations.MigrationService` reference a SQL Server resource. During local development the SQL Server resource is launched in a container.

## Demonstrates

- How to create migrations in an Aspire solution
- How to apply migrations in an Aspire solution

## Sample prerequisites

- [Aspire development environment](https://aspire.dev/get-started/prerequisites/)
- This sample is written in C# and targets .NET 10. It requires the [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.
- The `dotnet ef` .NET tool is required. It can be installed by running the following in a terminal:

    ```shell
    dotnet tool install --global dotnet-ef
    ```

## Create migration

The `DatabaseMigrations.MigrationService` project contains the EF Core migrations. The [`dotnet ef` command-line tool](https://learn.microsoft.com/ef/core/managing-schemas/migrations/#install-the-tools) can be used to create new migrations:

1. Update the `Entry` entity in database context in `MyDb1Context.cs`. Add a `Name` property:

    ```cs
    public class Entry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
    }
    ```

2. Open a command prompt in the `DatabaseMigrations.MigrationService` directory and run the EF Core migration tool to create a migration named **MyNewMigration**.

    ```bash
    dotnet ef migrations add MyNewMigration
    ```

    The preceding command:

      - Runs EF Core migration command-line tool in the `DatabaseMigrations.MigrationService` directory.
        - `dotnet ef` is run in this location because it will be used as the default target project for the new migration and the tool will run the startup code in `Program.cs` to find and configure the context to be used.
      - Creates the migration named `MyNewMigration` in the `DatabaseMigrations.MigrationService` project.

    > [!NOTE]
    > Other EF Core providers can impose different requirements for creating migrations. For example, the Npgsql provider requires a well-formed connection string to be present in the application's configuration, e.g. the `appsettings.Development.json` file, for the `DbContext` the migrations are being created for. The connection string need not be to an actual database, but it must be able to be successfully parsed. The SQL Server provider does not have this requirement.

3. View the new migration files in the `DatabaseMigrations.ApiModel` project.

> [!NOTE]
> To remove the unapplied migration you need to run `dotnet ef migrations remove --force`.  The `--force` switch tells the tool to avoid connecting to the database

## Run the app

If using the Aspire CLI, run `aspire run` from this directory.

If using VS Code, open this directory as a workspace and launch the `DatabaseMigrations.AppHost` project using either the Aspire or C# debuggers.

If using Visual Studio, open the solution file `DatabaseMigrations.slnx` and launch/debug the `DatabaseMigrations.AppHost` project.

If using the .NET CLI, run `dotnet run` from the `DatabaseMigrations.AppHost` directory.

When the app starts up, the `DatabaseMigrations.MigrationService` background worker runs migrations on the SQL Server container. The migration service:

- Creates a database in the SQL Server container.
- Creates the database schema.
- Stops itself once the migration is complete.
