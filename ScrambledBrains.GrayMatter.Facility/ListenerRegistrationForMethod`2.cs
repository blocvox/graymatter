using System;
using Castle.MicroKernel.Registration;

namespace ScrambledBrains.GrayMatter.Facility {
    public class ListenerRegistrationForMethod<TListener, TEvent> where TListener : class {
        private readonly ComponentRegistration<TListener> _componentRegistration;

        public ListenerRegistrationForMethod(ComponentRegistration<TListener> componentRegistration) {
            _componentRegistration = componentRegistration;
        }

        public ComponentRegistration<TListener> With(Action<TListener, TEvent> handler) {
            _componentRegistration.ExtendedProperties(Property.
                ForKey(GrayMatterFacility.CreateExtendedPropertyKey(typeof(TEvent), null, null)).
                Eq(new Wiring(typeof (TEvent), handler))
            );

            return _componentRegistration;
        }
    }
}