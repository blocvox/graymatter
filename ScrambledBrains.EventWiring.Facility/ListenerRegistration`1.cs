using Castle.MicroKernel.Registration;

namespace ScrambledBrains.EventWiring.Facility {
    public class ListenerRegistration<TComponent> where TComponent : class {
        private readonly ComponentRegistration<TComponent> _componentRegistration;

        public ListenerRegistration(ComponentRegistration<TComponent> componentRegistration) {
            _componentRegistration = componentRegistration;
        }

        public ListenerRegistrationForMethod<TComponent, TEvent> Event<TEvent>() {
            return new ListenerRegistrationForMethod<TComponent, TEvent>(_componentRegistration);
        }
    }
}