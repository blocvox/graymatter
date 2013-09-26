using System;

namespace ScrambledBrains.EventWiring.Facility {
    public partial class EventWiringFacility {
        private class EventWiringFacilityEventInfo {
            public EventWiringFacilityEventInfo(Delegate wireUpHandlerAction, Type eventType) {
                WireUpHandlerAction = wireUpHandlerAction;
                Type = eventType;
            }

            public Delegate WireUpHandlerAction { get; private set; }
            public Type Type { get; private set; }
        }
    }
}