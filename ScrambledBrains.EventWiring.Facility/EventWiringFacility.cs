using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel;

namespace ScrambledBrains.EventWiring.Facility {
    /* This is a listener-oriented facility; at the time of registration in
     * Windsor, a component of type TComponent can include extended
     * configuration (of type Provider) specifying
     *     A) an event delegate type TEvent, and
     *     B) a MethodInfo for the handling method.
     * The facility hooks into Windsor's ComponentModelCreated and
     * ComponentCreated kernel events to work its magic.
     *
     * During ComponentModelCreated, the facility builds a dictionary of all
     * wirings for each event type, by examining each registration for a
     * Wiring object.
     *
     * On every ComponentCreated, the facility will find any events on the
     * component type for which we have wirings. It will then attach a
     * "lazy" listener to the event for each wiring. This lazy listener,
     * when invoked, will
     *     1) resolve the wiring component from the container,
     *     2) pass it into the factory Expression to obtain the closed listener
     *        delegate instance,
     *     3) invoke the listener instance, and
     *     4) release the component.
     * This lazy listener hedges against event thats are not necessarily raised.
     * This way we don't incur the cost of resolving the wiring component
     * (which might entail construction of a large object graph) unless the
     * event is actually fired. Also, this means that we decouple the lifetimes
     * of the provider and listener; if the listener was shorter-lived than the
     * provider, the listener would be responsible for detaching from the event,
     * lest it leak memory.
     *
     * There's a fair bit of reflection and expression tree building here. The
     * process of finding events and attaching lazy listeners makes heavy use of
     * reflection. In order to maintain a high-level of performance, we perform
     * the reflection once for each component type and use it to build custom
     * expression trees (which do the actual finding of events and attachment
     * of a lazy listener). We compile these trees (for each provider type and
     * listener type) into delegates which are cached for invocation on
     * subsequent component creations.
     *
     * Currently, we create these delegates only the first time a particular
     * component type is created. This means that components with "late"
     * registrations that specify wirings for events exposed by types
     * which have already been created, will not be notified when those
     * types raise any events. To bring this behavior to the forefront, we
     * can explicitly "Freeze" the facility after all wiring
     * regsitrations have been processed. (This constraint can be eliminated by
     * implementing logic to compile/cache expression trees during
     * ComponentModelCreated for all types already having some cached
     * expression trees.)
     *
     * Assumptions:
     *     1) The listener will be resolved from the container every time the
     *        event is raised. (Depending on the listener's container
     *        registration, a different listener instance might handle the event
     *        each time its raised by the same provider.) This essentially set
     *        up weak references to the handling methods, so long-lived
     *        providers don't prevent short-lived listeners from being garbage
     *        collected.
     *     2) Event delegates must be of type Action<T> where T : EventBase.
     *        This is codified by GetAllActionEvents. It's possible to change
     *        this to allow arbitrary delegate signatures.
     *     3) Events are uniquely identifiable from their Action<> type's
     *        type parameter. In other words, wirings cannot differentiate
     *        on event name, just event type. Shouldn't be too hard to support
     *        this... allow greater event specification in the "Event()" fluent
     *        interface method, pull that into an extended EventInfo class, and
     *        filter against it in the outer loop of CompileSetupActions.
     *     4) Event listeners must catch/handle all exceptions.
     *
     * Inspired by <http://mikehadlow.blogspot.com/2010/01/10-advanced-windsor-tricks-7-how-to.html>.
     */
    public partial class EventWiringFacility : IFacility {
        private const string _WIRING_PROPERTY_KEY = "EventWiring";
        private static readonly Type _FACILITY_TYPE = typeof (EventWiringFacility);

        private bool _isFrozen;
        private IKernel _kernel;

        private readonly IDictionary<Type, ICollection<EventWiringFacilityListenerInfo>> _listeners =
            new Dictionary<Type, ICollection<EventWiringFacilityListenerInfo>>()
        ;
        private readonly IDictionary<Type, IEnumerable<Action<object>>> _eventWiringActionsByComponent =
            new Dictionary<Type, IEnumerable<Action<object>>>()
        ;

        public void Init(IKernel kernel, IConfiguration facilityConfig) {
            _kernel = kernel;
            kernel.ComponentModelCreated += KernelOnComponentModelCreated;
            kernel.ComponentCreated += KernelOnComponentCreated;
        }

        private void KernelOnComponentModelCreated(ComponentModel model) {
            var wiringKeys = model.ExtendedProperties.Keys.
                Cast<string>().
                Where(k => k.StartsWith(_WIRING_PROPERTY_KEY))
            ;

            foreach (var key in wiringKeys) {
                if (_isFrozen) throw new InvalidOperationException("A Component was registered after the EventWiringFacility was frozen. Late registrations are not allowed, to ensure that all listeners will receive all event notifications.");

                var wiring = (Wiring) model.ExtendedProperties[key];
                if (!_listeners.ContainsKey(wiring.EventType)) {
                    _listeners.Add(wiring.EventType, new List<EventWiringFacilityListenerInfo>());
                }

                var info = new EventWiringFacilityListenerInfo(model.Name, model.Services.Single(), wiring.HandleAction);
                _listeners[wiring.EventType].Add(info);
            }
        }

        private void KernelOnComponentCreated(ComponentModel model, object instance) {
            IEnumerable<Action<object>> eventWiringActions;

            // The basic idea here is that we need type-specific custom logic to hook up event handlers.
            // In order to avoid calls to DynamicInvoke (poor performance), we dynamically construct
            // the type-specific logic with reflection and expression trees, compile it, and cache it.
            if (!_eventWiringActionsByComponent.TryGetValue(model.Implementation, out eventWiringActions)) {
                var componentEvents = GetAllActionEvents(model);
                _eventWiringActionsByComponent[model.Implementation] = eventWiringActions = GetHandlerWiringActions(componentEvents, model.Implementation);
            }

            foreach(var eventWiringAction in eventWiringActions) eventWiringAction(instance);
        }

        private IEnumerable<Action<object>> GetHandlerWiringActions(IEnumerable<EventWiringFacilityEventInfo> componentEvents, Type providerType) {
            var setupActions = new List<Action<object>>();
            foreach (var eventMeta in componentEvents) {
                setupActions.Add((Action<object>)_FACILITY_TYPE.
                    GetMethod("GetCompositeEventWiringAction", BindingFlags.Instance | BindingFlags.NonPublic).
                    MakeGenericMethod(providerType, eventMeta.Type).
                    Invoke(this, new[] {eventMeta.WireUpHandlerAction})
                );
            }
            return setupActions;
        }

        // Invoked by reflection.
        private Action<object> GetCompositeEventWiringAction<TProvider, TEvent>(Action<TProvider, Action<TEvent>> componentWireUpHandlerAction) {
            var handlerWiringActions = new List<Action<TProvider>>();
            foreach (var listener in _listeners[typeof(TEvent)]) {
                var /*Action<TProvider>*/ eventSetupAction = (Action<TProvider>)(_FACILITY_TYPE.
                    GetMethod("GetHandlerEventWiringAction", BindingFlags.Instance | BindingFlags.NonPublic).
                    MakeGenericMethod(listener.Type, typeof(TEvent), typeof(TProvider)).
                    Invoke(this, new object[]{listener.ComponentId, listener.HandleAction, componentWireUpHandlerAction})
                );

                handlerWiringActions.Add(eventSetupAction);
            }

            // This will be invoked in KernelOnComponentCreated. The point is to cast all untyped delegates back to
            // a strong type and close over those references, and then to invoke them with the same casted TListener
            // instance. This way we need only perform one cast per component creation.
            Action<object> setupAction = component => {
                var eventProvider = (TProvider) component;
                foreach (var wireUpHandler in handlerWiringActions) wireUpHandler(eventProvider);
            };

            return setupAction;
        }

        // Invoked by reflection.
        private Action<TProvider> GetHandlerEventWiringAction<TListener, TEvent, TProvider>(string componentId, Delegate handleActionDelegate, Action<TProvider, Action<TEvent>> wireUpHandler) {
            var handle = (Action<TListener, TEvent>) handleActionDelegate;
            return provider => wireUpHandler(
                provider,
                arg => {
                    var listener = _kernel.Resolve<TListener>(componentId);
                    try { handle(listener, arg); }
                    finally { _kernel.ReleaseComponent(listener); }
                }
            );
        }

        private static IEnumerable<EventWiringFacilityEventInfo> GetAllActionEvents(ComponentModel model) {
            var coll = new List<EventWiringFacilityEventInfo>();

            // Find all potential event property add methods (these are the methods that the compiler
            // generates for us when we use the 'event' keyword). Need to use this technique instead
            // of directly adding each handler to eventInfo with AddEventHandler bc .NET 4.0 multicast
            // delegate contravariance is broken.
            var wireUpMethodCandidates = model.Implementation.
                GetMethods(BindingFlags.Public | BindingFlags.Instance).
                Where(mi => mi.Name.StartsWith("add_") || mi.Name.Contains(".add_")).
                Where(mi => mi.GetParameters().Count() == 1)
            ;

            foreach (var method in wireUpMethodCandidates) {
                var paramType = method.GetParameters().Single().ParameterType;
                if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof (Action<>)) continue;

                // This returns a delegate that is invoked in delegate returned by CreateSetupAction and looks like:
                // (TProvider provider, TEvent handler) => provider.Add_Raised(handler);
                var providerX = Expression.Parameter(model.Implementation);
                var handlerParamX = Expression.Parameter(paramType);
                var callX = Expression.Call(providerX, method, handlerParamX);
                var lambdaX = Expression.Lambda(callX, providerX,handlerParamX);
                var wireUpHandlerAction = lambdaX.Compile();

                coll.Add(new EventWiringFacilityEventInfo(wireUpHandlerAction, paramType.GetGenericArguments().Single()));
            }
            return coll;
        }

        public void Freeze() { _isFrozen = true; }
        public void Terminate() {}

        public static string CreateExtendedPropertyKey(Type @event, string handlerType, string handlerMethodName) {
            return string.Format("{0}|{1}|{2}|{3}", _WIRING_PROPERTY_KEY, @event.FullName, handlerType ?? Guid.NewGuid().ToString(), handlerMethodName ?? "?");
        }
    }
}