﻿using System;

namespace ScrambledBrains.EventWiring.Sample {
    internal class Publisher {
        public void DoSomething() {
            // Does something...

            OnSomethingOccurred();
        }

        public event Action<SomethingOccurrence> SomethingOccurred;

        public void OnSomethingOccurred() {
            var handler = SomethingOccurred;
            if (handler != null) handler(new SomethingOccurrence());
        }
    }
}