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

        public ComponentRegistration<THandler> With(Expression<Action<THandler, TEvent>> handler) {
            var callX = (MethodCallExpression) handler.Body;

            Debug.Assert(callX.Method.ReturnType == typeof(void));
            Debug.Assert(callX.Method.GetParameters().Single().ParameterType == typeof(TEvent));

            _componentRegistration.ExtendedProperties(Property.
                ForKey(EventWiringFacility.CreateExtendedPropertyKey(typeof(TEvent), callX.Method)).
                Eq(new Subscription(typeof (TEvent), callX.Method))
            );

            return _componentRegistration;
        }
    }
}