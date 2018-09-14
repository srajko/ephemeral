using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TSql;

namespace Ephemeral
{
    public class EphemeralManager
    {
        string DatabaseConnectionString;

        public EphemeralManager(string databaseConnectionString)
        {
            DatabaseConnectionString = databaseConnectionString;
            using (var context = new EphemeralDbContext(DatabaseConnectionString))
            {
                context.Database.Migrate();
            }
        }

        public TestDatabase GetTestDatabase(string databaseScript, Variant variant = Variant.Default, string dataScript = null)
        {
            var sha512 = SHA512Managed.Create();
            var bytes = Encoding.UTF8.GetBytes(databaseScript);
            var hash = sha512.ComputeHash(bytes);

            bool createDatabase = false;
            string connectionString;
            int? id = null;

            using (var context = new EphemeralDbContext(DatabaseConnectionString))
            {
                context.Database.Migrate();
                using (var transaction = context.Database.BeginTransaction())
                {
                    var database = context.EphemeralDatabases.Where(db => db.VersionHash == hash && db.Variant == variant && db.CheckedOut == null).FirstOrDefault();
                    if (database != null)
                    {
                        database.CheckedOut = DateTimeOffset.UtcNow;
                        context.SaveChanges();
                        id = database.Id;
                    }
                    transaction.Commit();
                }
                if (id == null)
                {
                    var database = context.EphemeralDatabases.Add(new EphemeralDatabase
                    {
                        VersionHash = hash,
                        Variant = variant,
                        CheckedOut = DateTimeOffset.UtcNow
                    });
                    context.SaveChanges();
                    createDatabase = true;
                    id = database.Entity.Id;
                }
                connectionString = GetConnectionString(id.Value);
            }

            using (var newDbContext = ContextForConnectionString(connectionString))
            {
                if (createDatabase)
                {
                    newDbContext.Database.EnsureCreated();

                    if (variant == Variant.MemoryOptimized)
                    {
                        var databaseFiles = newDbContext.DatabaseFiles.FromSql("SELECT * FROM sys.database_files").ToList();

                        var builder = new SqlConnectionStringBuilder(connectionString);
                        var turnAutoCloseOffSql = $"ALTER DATABASE [{builder.InitialCatalog}]  SET AUTO_CLOSE OFF";
                        var addFilegroupSql = $"ALTER DATABASE [{builder.InitialCatalog}] ADD FILEGROUP memory_optimized CONTAINS MEMORY_OPTIMIZED_DATA";
                        var addFileSql = $"ALTER DATABASE [{builder.InitialCatalog}] ADD FILE (name='memory_optimized_file', filename='{databaseFiles.First().PhysicalName}.mem') TO FILEGROUP memory_optimized";
                        newDbContext.Database.ExecuteSqlCommand(turnAutoCloseOffSql);
                        newDbContext.Database.ExecuteSqlCommand(addFilegroupSql);
                        newDbContext.Database.ExecuteSqlCommand(addFileSql);
                    }

                    foreach (var (batch, batchSql) in TSqlUtilities.GetBatches(databaseScript))
                    {
                        var sql = batchSql;
                        if (variant == Variant.MemoryOptimized)
                        {
                            var visitor = new MemoryOptimizedVisitor();
                            visitor.Visit(batch);
                            sql = visitor.Transform(batchSql, batch.Start.StartIndex);
                        }

                        try
                        {
                            newDbContext.Database.ExecuteSqlCommand(sql);
                        } catch (Exception exception)
                        {
                            throw new Exception($"Failed executing SQL command\n{sql}", exception);
                        }
                    }
                }
                else
                {
                    // delete data
                    newDbContext.Database.ExecuteSqlCommand(@"
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
EXEC sp_MSForEachTable 'DELETE FROM ?'
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
                }
                if (dataScript != null)
                {
                    // run the initial data script
                    foreach (var (batch, batchSql) in TSqlUtilities.GetBatches(dataScript))
                    {
                        newDbContext.Database.ExecuteSqlCommand(batchSql);
                    }
                }
            }
            return new TestDatabase(connectionString, id.Value, DatabaseConnectionString);
        }

        public void DeleteAllDatabases()
        {
            using (var context = new EphemeralDbContext(DatabaseConnectionString))
            {
                var databases = context.EphemeralDatabases.ToList();
                context.EphemeralDatabases.RemoveRange(context.EphemeralDatabases);
                context.SaveChanges();

                foreach (var database in databases)
                {
                    using (var ephemeralContext = ContextForConnectionString(GetConnectionString(database.Id)))
                    {
                        ephemeralContext.Database.EnsureDeleted();
                    }
                }
            }
        }

        private string GetConnectionString(int id)
        {
            var builder = new SqlConnectionStringBuilder(DatabaseConnectionString);
            builder.InitialCatalog += "-" + id;
            return builder.ToString();
        }

        private TestDatabaseDbContext ContextForConnectionString(string connectionString)
        {
            return
                new TestDatabaseDbContext(
                    new DbContextOptionsBuilder<DbContext>()
                        .UseSqlServer(connectionString)
                        .Options
                );
        }
    }
}
