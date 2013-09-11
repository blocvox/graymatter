using System;
using System.Reflection;

namespace ScrambledBrains.EventWiring.Facility {
    public partial class EventWiringFacility {
        private class EventWiringFacilityEventInfo {
            public EventWiringFacilityEventInfo(MethodInfo addHandler, Type eventType) {
                AddHandler = addHandler;
                EventType = eventType;
            }

            public MethodInfo AddHandler { get; private set; }
            public Type EventType { get; private set; }
        }
    }
}