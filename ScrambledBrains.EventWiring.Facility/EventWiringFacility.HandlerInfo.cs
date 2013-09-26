using System;

namespace ScrambledBrains.EventWiring.Facility {
    public partial class EventWiringFacility {
        private class EventWiringFacilityHandlerInfo {
            public string ComponentId { get; private set; }
            public Type ServiceType { get; private set; }
            public Delegate Handler { get; private set; }

            public EventWiringFacilityHandlerInfo(string componentId, Type serviceType, Delegate handler) {
                ComponentId = componentId;
                ServiceType = serviceType;
                Handler = handler;
            }
        }
    }
}