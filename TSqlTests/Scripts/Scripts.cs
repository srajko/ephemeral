using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TSqlTests
{
    public static class Scripts
    {
        public static string AdventureWorksCreate()
        {
            var assembly = typeof(Scripts).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream("TSqlTests.Scripts.AdventureWorksCreate.sql"))
            using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
