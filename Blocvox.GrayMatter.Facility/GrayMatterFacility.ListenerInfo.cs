using System;

namespace Blocvox.GrayMatter.Facility {
    public partial class GrayMatterFacility {
        private class GrayMatterFacilityListenerInfo {
            public string ComponentId { get; private set; }
            public Type Type { get; private set; }
            public Delegate HandleAction { get; private set; }

            public GrayMatterFacilityListenerInfo(string componentId, Type listener, Delegate handleAction) {
                ComponentId = componentId;
                Type = listener;
                HandleAction = handleAction;
            }
        }
    }
}