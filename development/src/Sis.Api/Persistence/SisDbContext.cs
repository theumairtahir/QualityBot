using Microsoft.EntityFrameworkCore;

namespace Sis.Api.Persistence;

public sealed class SisDbContext(DbContextOptions<SisDbContext> options) : DbContext(options)
{
    // Each feature adds its own IEntityTypeConfiguration<T>; nothing to register by hand.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SisDbContext).Assembly);
}
