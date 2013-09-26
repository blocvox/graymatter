using System;

namespace ScrambledBrains.GrayMatter.Facility {
    public partial class GrayMatterFacility {
        private class GrayMatterFacilityEventInfo {
            public GrayMatterFacilityEventInfo(Delegate wireUpHandlerAction, Type eventType) {
                WireUpHandlerAction = wireUpHandlerAction;
                Type = eventType;
            }

            public Delegate WireUpHandlerAction { get; private set; }
            public Type Type { get; private set; }
        }
    }
}