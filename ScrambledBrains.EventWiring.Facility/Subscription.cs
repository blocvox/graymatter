using System;
using System.Reflection;

namespace ScrambledBrains.EventWiring.Facility {
    public class Subscription {
        public Type EventType { get; private set; }
        public MethodInfo Handler { get; private set; }

        public Subscription(Type eventType, MethodInfo handler) {
            EventType = eventType;
            Handler = handler;
        }
    }
}