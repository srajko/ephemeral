using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;


namespace Ephemeral
{
    class TestDatabaseDbContext : DbContext
    {
        public TestDatabaseDbContext(DbContextOptions options) : base(options)
        { }

        public DbQuery<DatabaseFiles> DatabaseFiles { get; set; }
    }

    class DatabaseFiles
    {
        [Column("physical_name")]
        public string PhysicalName { get; set; }
    }
}
