using System;
using System.Linq;


namespace Ephemeral
{
    public class TestDatabase : IDisposable
    {
        public string ConnectionString { get; private set; }

        int EphemeralDatabaseId;

        string EphemeralConnectionString;

        public TestDatabase(string connectionString, int ephemeralDatabaseId, string ephemeralConnectionString)
        {
            ConnectionString = connectionString;
            EphemeralDatabaseId = ephemeralDatabaseId;
            EphemeralConnectionString = ephemeralConnectionString;
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
        #endregion
    }
}
