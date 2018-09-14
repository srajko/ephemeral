using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

        public TestDatabase GetTestDatabase(string schemaScript, Variant variant = Variant.Default, string dataScript = null)
        {
            var sha512 = SHA512Managed.Create();
            var bytes = Encoding.UTF8.GetBytes(schemaScript);
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

            var testDatabase = new TestDatabase(connectionString, id.Value, DatabaseConnectionString, dataScript);
            if (createDatabase)
            {
                testDatabase.Create(schemaScript, variant);
            }
            testDatabase.ResetData();

            return testDatabase;
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
                    new TestDatabase(GetConnectionString(database.Id), database.Id, DatabaseConnectionString, null).Delete();
                }
            }
        }

        private string GetConnectionString(int id)
        {
            var builder = new SqlConnectionStringBuilder(DatabaseConnectionString);
            builder.InitialCatalog += "-" + id;
            return builder.ToString();
        }        
    }
}
