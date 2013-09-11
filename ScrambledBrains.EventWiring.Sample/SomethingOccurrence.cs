using System;

namespace ScrambledBrains.EventWiring.Sample {
    internal class SomethingOccurrence {
        public SomethingOccurrence() { At = DateTime.UtcNow; }

        public DateTime At { get; set; }
    }
}