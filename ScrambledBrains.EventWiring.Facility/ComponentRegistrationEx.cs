using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Registration;

namespace ScrambledBrains.EventWiring.Facility {
    public static class ComponentRegistrationEx {
        // Usage: Component.For<Something>().SubscribesTo().Event<InterestingEvent>() ...
        public static SubscriptionRegistration<TComponent> SubscribesTo<TComponent>(
            this ComponentRegistration<TComponent> registration
        ) where TComponent : class {
            return new SubscriptionRegistration<TComponent>(registration);
        }

        public static ComponentRegistration<TComponent> SubscribesToEvent<TComponent>(
            this ComponentRegistration<TComponent> registration,
            Type eventType,
            MethodInfo handler
        ) where TComponent : class {
            Debug.Assert(handler != null);
            Debug.Assert(handler.ReturnType == typeof(void));
            Debug.Assert(handler.GetParameters().Single().ParameterType == eventType);

            registration.ExtendedProperties(Property.
                ForKey(EventWiringFacility.CreateExtendedPropertyKey(eventType, handler)).
                Eq(new Subscription(eventType, handler))
            );

            return registration;
        }

        public static ComponentRegistration<TComponent> SubscribesToEvent<TComponent>(
            this ComponentRegistration<TComponent> registration,
            Type eventType,
            string methodName
        ) where TComponent : class {
            return registration.SubscribesToEvent(
                eventType,
                typeof(TComponent).GetMethod(methodName, new[] { eventType })
            );
        }
    }
}