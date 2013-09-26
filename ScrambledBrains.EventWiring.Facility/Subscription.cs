using System;

namespace ScrambledBrains.EventWiring.Facility {
    public class Subscription {
        public Type EventType { get; private set; }

        // Action<THandler, TEvent>
        public Delegate Handler { get; private set; }

        public Subscription(Type eventType, Delegate handler) {
            EventType = eventType;
            Handler = handler;
        }
    }
}