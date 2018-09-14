using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Linq;
using TSql;

namespace Ephemeral
{
    public class TestDatabase : IDisposable
    {
        public string ConnectionString { get; private set; }

        int EphemeralDatabaseId;

        string EphemeralConnectionString;

        string DataScript;

        public TestDatabase(string connectionString, int ephemeralDatabaseId, string ephemeralConnectionString, string dataScript)
        {
            ConnectionString = connectionString;
            EphemeralDatabaseId = ephemeralDatabaseId;
            EphemeralConnectionString = ephemeralConnectionString;
            DataScript = dataScript;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        public void Dispose()
        {
            if (!disposedValue)
            {
                using (var context = new EphemeralDbContext(EphemeralConnectionString))
                {
                    var database = context.EphemeralDatabases.Where(db => db.Id == EphemeralDatabaseId).FirstOrDefault();
                    if (database != null)
                    {
                        database.CheckedOut = null;
                        context.SaveChanges();
                    }
                }

                disposedValue = true;
            }
        }

        internal void Create(string schemaScript, Variant variant)
        {
            using (var dbContext = DbContext())
            {
                dbContext.Database.EnsureCreated();

                if (variant == Variant.MemoryOptimized)
                {
                    var databaseFiles = dbContext.DatabaseFiles.FromSql("SELECT * FROM sys.database_files").ToList();

                    var builder = new SqlConnectionStringBuilder(ConnectionString);
                    var turnAutoCloseOffSql = $"ALTER DATABASE [{builder.InitialCatalog}]  SET AUTO_CLOSE OFF";
                    var addFilegroupSql = $"ALTER DATABASE [{builder.InitialCatalog}] ADD FILEGROUP memory_optimized CONTAINS MEMORY_OPTIMIZED_DATA";
                    var addFileSql = $"ALTER DATABASE [{builder.InitialCatalog}] ADD FILE (name='memory_optimized_file', filename='{databaseFiles.First().PhysicalName}.mem') TO FILEGROUP memory_optimized";
                    dbContext.Database.ExecuteSqlCommand(turnAutoCloseOffSql);
                    dbContext.Database.ExecuteSqlCommand(addFilegroupSql);
                    dbContext.Database.ExecuteSqlCommand(addFileSql);
                }

                foreach (var (batch, batchSql) in TSqlUtilities.GetBatches(schemaScript))
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
                        dbContext.Database.ExecuteSqlCommand(sql);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Failed executing SQL command\n{sql}", exception);
                    }
                }
            }
        }

        internal void Delete()
        {
            using (var dbContext = DbContext())
            {
                dbContext.Database.EnsureDeleted();
            }
        }

        public void ResetData()
        {
            using (var dbContext = DbContext())
            {
                // delete data
                dbContext.Database.ExecuteSqlCommand(@"
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; DELETE FROM ?'
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

                if (DataScript != null)
                {
                    // run the initial data script
                    foreach (var (batch, batchSql) in TSqlUtilities.GetBatches(DataScript))
                    {
                        dbContext.Database.ExecuteSqlCommand(batchSql);
                    }
                }
            }
        }

        private TestDatabaseDbContext DbContext()
        {
            return
                new TestDatabaseDbContext(
                    new DbContextOptionsBuilder<DbContext>()
                        .UseSqlServer(ConnectionString)
                        .Options
                );
        }
        #endregion
    }
}
