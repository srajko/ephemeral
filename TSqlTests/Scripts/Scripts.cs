using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TSqlTests
{
    public static class Scripts
    {
        public static string EphemeralCreate()
        {
            return Script("TSqlTests.Scripts.EphemeralCreate.sql");
        }

        public static string EphemeralData()
        {
            return Script("TSqlTests.Scripts.EphemeralData.sql");
        }

        public static string SimpleCreate()
        {
            return Script("TSqlTests.Scripts.SimpleCreate.sql");
        }

        public static string AdventureWorksCreate()
        {
            return Script("TSqlTests.Scripts.AdventureWorksCreate.sql");
        }

        private static string Script(string name)
        {
            var assembly = typeof(Scripts).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
