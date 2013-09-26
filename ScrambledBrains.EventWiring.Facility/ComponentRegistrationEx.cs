using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.MicroKernel.Registration;

namespace ScrambledBrains.EventWiring.Facility {
    public static class ComponentRegistrationEx {
        // Usage: Component.For<Something>().ListensTo().Event<InterestingEvent>() ...
        public static ListenerRegistration<TComponent> ListensTo<TComponent>(
            this ComponentRegistration<TComponent> registration
        ) where TComponent : class {
            return new ListenerRegistration<TComponent>(registration);
        }

        public static ComponentRegistration<TComponent> ListensToEvent<TComponent>(
            this ComponentRegistration<TComponent> registration,
            Type eventType,
            MethodInfo handleMethod
        ) where TComponent : class {
            Debug.Assert(handleMethod != null);
            Debug.Assert(handleMethod.ReturnType == typeof(void));
            Debug.Assert(handleMethod.GetParameters().Single().ParameterType == eventType);

            var /*Action<TListener, TEvent>*/ @delegate = (Delegate)(typeof(ComponentRegistrationEx).
                GetMethod("CreateInvoker", BindingFlags.Static | BindingFlags.NonPublic).
                MakeGenericMethod(typeof(TComponent),eventType).
                Invoke(null, new object[]{handleMethod})
            );

            registration.ExtendedProperties(Property.
                ForKey(EventWiringFacility.CreateExtendedPropertyKey(eventType, handleMethod.ReflectedType.ToString(), handleMethod.Name)).
                Eq(new Wiring(eventType, @delegate))
            );

            return registration;
        }

        public static ComponentRegistration<TComponent> ListensToEvent<TComponent>(
            this ComponentRegistration<TComponent> registration,
            Type eventType,
            string methodName
        ) where TComponent : class {
            return registration.ListensToEvent(
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