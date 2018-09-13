using Ephemeral;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using TSqlTests;

namespace EphemeralTests
{
    [TestClass]
    public class EphemeralDatabaseTests
    {
        static string ephemeralConnectionString = @"Data Source=.\SQLExpress;Initial Catalog=Ephemeral;Integrated Security=True";
        static EphemeralManager EphemeralManager = new EphemeralManager(ephemeralConnectionString);

        [TestMethod]
        public void TestDatabaseCreationChecksOutCorrectly()
        {
            using (var testDatabase = EphemeralManager.GetTestDatabase(Scripts.EphemeralCreate(), Variant.Default, Scripts.EphemeralData()))
            {
                var manager = new EphemeralManager(testDatabase.ConnectionString);
                try
                {
                    using (var context = new EphemeralDbContext(testDatabase.ConnectionString))
                    {
                        // check out a new database
                        var beforeTestDatabaseCreation = DateTimeOffset.UtcNow;
                        using (var newDatabase = manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.Default))
                        {
                            var afterTestDatabaseCreation = DateTimeOffset.UtcNow;

                            Assert.AreEqual(1, context.EphemeralDatabases.AsNoTracking().Count());
                            var database = context.EphemeralDatabases.AsNoTracking().First();
                            Assert.AreEqual(Variant.Default, database.Variant);
                            Assert.IsTrue(beforeTestDatabaseCreation <= database.CheckedOut && database.CheckedOut <= afterTestDatabaseCreation);
                        }

                        Assert.AreEqual(1, context.EphemeralDatabases.AsNoTracking().Count());
                        Assert.IsNull(context.EphemeralDatabases.AsNoTracking().First().CheckedOut);
                    }
                }
                finally
                {
                    manager.DeleteAllDatabases();
                }
            }
        }

        [TestMethod]
        public void TestDatabaseCreationReusesDatabases()
        {
            using (var testDatabase = EphemeralManager.GetTestDatabase(Scripts.EphemeralCreate(), Variant.Default, Scripts.EphemeralData()))
            {
                var manager = new EphemeralManager(testDatabase.ConnectionString);
                try
                {
                    using (var context = new EphemeralDbContext(testDatabase.ConnectionString))
                    {
                        int id;

                        // check out a new database
                        using (manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.Default))
                        {
                            Assert.AreEqual(1, context.EphemeralDatabases.AsNoTracking().Count());
                            id = context.EphemeralDatabases.AsNoTracking().First().Id;
                        }

                        // check out a database again
                        using (manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.Default))
                        {
                            Assert.AreEqual(1, context.EphemeralDatabases.AsNoTracking().Count());
                            Assert.AreEqual(id, context.EphemeralDatabases.AsNoTracking().First().Id);
                        }
                    }
                }
                finally
                {
                    manager.DeleteAllDatabases();
                }
            }
        }

        [TestMethod]
        public void CompareDatabasePerformance()
        {
            using (var testDatabase = EphemeralManager.GetTestDatabase(Scripts.EphemeralCreate(), Variant.Default, Scripts.EphemeralData()))
            {
                var manager = new EphemeralManager(testDatabase.ConnectionString);
                try
                {
                    TimeSpan defaultDuration, memoryOptimizedDuration;

                    using (var newDatabase = manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.Default))
                    {
                        defaultDuration = TimePerformance(new SqlConnection(newDatabase.ConnectionString));
                    }

                    using (var newDatabase = manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.MemoryOptimized))
                    {
                        memoryOptimizedDuration = TimePerformance(new SqlConnection(newDatabase.ConnectionString));
                    }

                    Assert.IsTrue(memoryOptimizedDuration < defaultDuration);
                }
                finally
                {
                    manager.DeleteAllDatabases();
                }
            }
        }

        [TestMethod]
        public void TestDatabaseDeletion()
        {
            using (var testDatabase = EphemeralManager.GetTestDatabase(Scripts.EphemeralCreate(), Variant.Default, Scripts.EphemeralData()))
            {
                var manager = new EphemeralManager(testDatabase.ConnectionString);
                try
                {
                    using (var context = new EphemeralDbContext(testDatabase.ConnectionString))
                    {
                        // check out a new database
                        using (var newDatabase = manager.GetTestDatabase(Scripts.SimpleCreate(), Variant.Default))
                        {
                        }

                        Assert.AreEqual(1, context.EphemeralDatabases.AsNoTracking().Count());

                        // delete all databases
                        manager.DeleteAllDatabases();

                        Assert.AreEqual(0, context.EphemeralDatabases.AsNoTracking().Count());
                    }
                }
                finally
                {
                    manager.DeleteAllDatabases();
                }
            }
        }

        private TimeSpan TimePerformance(SqlConnection connection)
        {
            var repetitions = 10000;

            using (connection)
            {
                connection.Open();
                var start = DateTime.Now;
                for (var i = 0; i < repetitions; i++)
                {
                    using (var command = new SqlCommand("insert into People (FirstName, LastName) values ('First', 'Last')", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            Assert.AreEqual(1, reader.RecordsAffected);
                        }
                    }
                    using (var command = new SqlCommand("select * from People", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                        }
                    }
                }
                var duration = DateTime.Now - start;
                Console.WriteLine(duration);
                return duration;
            }
        }
    }
}
