using System;

namespace Blocvox.GrayMatter.Sample {
    public class UninterestingProvider {
        public event Action<object> SomethingBoringOccurred;

        protected virtual void OnSomethingBoringOccurred() {
            var handler = SomethingBoringOccurred;
            if (handler != null) handler("Zzz");
        }
    }
}