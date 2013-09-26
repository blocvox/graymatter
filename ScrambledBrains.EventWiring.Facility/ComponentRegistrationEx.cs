using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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

            var /*Action<THandler, TEvent>*/ @delegate = (Delegate)(typeof(ComponentRegistrationEx).
                GetMethod("CreateInvoker", BindingFlags.Static | BindingFlags.NonPublic).
                MakeGenericMethod(typeof(TComponent),eventType).
                Invoke(null, new object[]{handler})
            );

            registration.ExtendedProperties(Property.
                ForKey(EventWiringFacility.CreateExtendedPropertyKey(eventType, handler.ReflectedType.ToString(), handler.Name)).
                Eq(new Subscription(eventType, @delegate))
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

        // Invoked via reflection.
        private static Delegate CreateInvoker<TComponent, TEvent>(MethodInfo method) {
            var handlerParamX = Expression.Parameter(typeof(TComponent));
            var eventParamX = Expression.Parameter(typeof(TEvent));
            var callX = Expression.Call(handlerParamX, method, eventParamX);
            var lambda = Expression.Lambda(callX, handlerParamX, eventParamX);
            return lambda.Compile();
        }
    }
}