using System;
using System.Reflection;

namespace ScrambledBrains.EventWiring.Facility {
    public partial class EventWiringFacility {
        private class EventWiringFacilityHandlerInfo {
            public string ComponentId { get; private set; }
            public Type ServiceType { get; private set; }
            public MethodInfo Handler { get; private set; }

            public EventWiringFacilityHandlerInfo(string componentId, Type serviceType, MethodInfo handler) {
                ComponentId = componentId;
                ServiceType = serviceType;
                Handler = handler;
            }
        }
    }
}