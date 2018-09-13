using Ephemeral;
using Microsoft.EntityFrameworkCore.Design;

namespace EphemeralConsole
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class LoadDatabase : IDesignTimeDbContextFactory<EphemeralDbContext>
    {
        public EphemeralDbContext CreateDbContext(string[] args)
        {
            var connectionString = @"Data Source=.\SQLExpress;Initial Catalog=Ephemeral;Integrated Security=True";

            return new EphemeralDbContext(connectionString);
        }
    }
}
