using Microsoft.EntityFrameworkCore;

namespace VolumeMount.BlazorWeb.Data;

public class PostgresDbContext(DbContextOptions<PostgresDbContext> options) : DbContext(options)
{

}
