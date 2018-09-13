using System;

namespace Ephemeral
{
    public enum Variant
    {
        Default,
        MemoryOptimized
    }

    public class EphemeralDatabase
    {
        public int Id { get; set; }
        public byte[] VersionHash { get; set; }
        public Variant Variant { get; set; }
        public DateTimeOffset? CheckedOut { get; set; }
    }
}
