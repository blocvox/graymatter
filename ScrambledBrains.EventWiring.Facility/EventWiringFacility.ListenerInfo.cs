using System;

namespace ScrambledBrains.EventWiring.Facility {
    public partial class EventWiringFacility {
        private class EventWiringFacilityListenerInfo {
            public string ComponentId { get; private set; }
            public Type Type { get; private set; }
            public Delegate HandleAction { get; private set; }

            public EventWiringFacilityListenerInfo(string componentId, Type listener, Delegate handleAction) {
                ComponentId = componentId;
                Type = listener;
                HandleAction = handleAction;
            }
        }
    }
}