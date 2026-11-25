using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DermaCare.Data;

public class DermaDbContextFactory : IDesignTimeDbContextFactory<DermaDbContext>
{
    public DermaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DermaDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=DermaCareDb;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new DermaDbContext(optionsBuilder.Options);
    }
}
