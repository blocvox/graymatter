using System;

namespace ScrambledBrains.GrayMatter.Facility {
    public class Wiring {
        public Type EventType { get; private set; }

        // Action<TListener, TEvent>
        public Delegate HandleAction { get; private set; }

        public Wiring(Type eventType, Delegate handleAction) {
            EventType = eventType;
            HandleAction = handleAction;
        }
    }
}