using System;

namespace Blocvox.GrayMatter.Sample {
    internal class Provider {
        public void DoSomething() {
            // Does something...

            OnSomethingOccurred();
        }

        public event Action<SomethingOccurrence> SomethingOccurred;

        protected virtual void OnSomethingOccurred() {
            var handler = SomethingOccurred;
            if (handler != null) handler(new SomethingOccurrence());
        }
    }
}