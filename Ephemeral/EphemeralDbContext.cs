using Microsoft.EntityFrameworkCore;

namespace Ephemeral
{
    public class EphemeralDbContext : DbContext
    {
        public EphemeralDbContext(string connectionString)
            : base(new DbContextOptionsBuilder<EphemeralDbContext>().UseSqlServer(
               connectionString
            ).Options)
        {
        }
        public DbSet<EphemeralDatabase> EphemeralDatabases { get; set; }
    }
}
