using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Castle.MicroKernel.Registration;

namespace ScrambledBrains.EventWiring.Facility {
    public class SubscriptionRegistrationForMethod<THandler, TEvent> where THandler : class {
        private readonly ComponentRegistration<THandler> _componentRegistration;

        public SubscriptionRegistrationForMethod(ComponentRegistration<THandler> componentRegistration) {
            _componentRegistration = componentRegistration;
        }

        public ComponentRegistration<THandler> With(Action<THandler, TEvent> handler) {
            _componentRegistration.ExtendedProperties(Property.
                ForKey(EventWiringFacility.CreateExtendedPropertyKey(typeof(TEvent), null, null)).
                Eq(new Subscription(typeof (TEvent), handler))
            );

            return _componentRegistration;
        }
    }
}