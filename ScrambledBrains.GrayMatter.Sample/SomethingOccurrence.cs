using System;

namespace ScrambledBrains.GrayMatter.Sample {
    internal class SomethingOccurrence {
        public SomethingOccurrence() { At = DateTime.UtcNow; }

        public DateTime At { get; set; }
    }
}