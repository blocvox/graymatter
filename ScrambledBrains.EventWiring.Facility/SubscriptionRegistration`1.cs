using Castle.MicroKernel.Registration;

namespace ScrambledBrains.EventWiring.Facility {
    public class SubscriptionRegistration<TComponent> where TComponent : class {
        private readonly ComponentRegistration<TComponent> _componentRegistration;

        public SubscriptionRegistration(ComponentRegistration<TComponent> componentRegistration) {
            _componentRegistration = componentRegistration;
        }

        public SubscriptionRegistrationForMethod<TComponent, TEvent> Event<TEvent>() {
            return new SubscriptionRegistrationForMethod<TComponent, TEvent>(_componentRegistration);
        }
    }
}