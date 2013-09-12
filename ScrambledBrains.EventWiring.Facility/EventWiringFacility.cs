using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.MicroKernel;

namespace ScrambledBrains.EventWiring.Facility {
    /* This is a handler-oriented facility; at the time of registration in
     * Windsor, a component of type TComponent can include extended
     * configuration (of type Subscription) specifying
     *     A) an event delegate type TEvent, and
     *     B) a MethodInfo for the handling method.
     * The facility hooks into Windsor's ComponentModelCreated and
     * ComponentCreated kernel events to work its magic.
     *
     * During ComponentModelCreated, the facility builds a dictionary of all
     * subscriptions for each event type, by examining each registration for a
     * SubscriptionConfiguration object.
     *
     * On every ComponentCreated, the facility will find any events on the
     * component type for which we have subscriptions. It will then attach a
     * "lazy" handler to the event for each subscription. This lazy handler,
     * when invoked, will
     *     1) resolve the subscribing component from the container,
     *     2) pass it into the factory Expression to obtain the closed handler
     *        delegate instance,
     *     3) invoke the handler instance, and
     *     4) release the component.
     * This lazy handler hedges against event thats are not necessarily raised.
     * This way we don't incur the cost of resolving the subscribing component
     * (which might entail construction of a large object graph) unless the
     * event is actually fired. Also, this means that we decouple the lifetimes
     * of the raiser and handler; if the handler was shorter-lived than the
     * raiser, the handler would be responsible for unsubscribing to the event,
     * lest it leak memory.
     *
     * There's a fair bit of reflection and expression tree building here. The
     * process of finding events and attaching lazy handlers makes heavy use of
     * reflection. In order to maintain a high-level of performance, we perform
     * the reflection once for each component type and use it to build custom
     * expression trees (which do the actual finding of events and attachment
     * of a lazy handler). We compile these trees (for each component type and
     * subscription type) into delegates which are cached for invocation on
     * subsequent component creations.
     *
     * Currently, we create these delegates only the first time a particular
     * component type is created. This means that components with "late"
     * registrations that specify subscriptions for events exposed by types
     * which have already been created, will not be notified when those
     * types raise any events. To bring this behavior to the forefront, we
     * can explicitly "Freeze" the facility after all subscribing
     * regsitrations have been processed. (This constraint can be eliminated by
     * implementing logic to compile/cache expression trees during
     * ComponentModelCreated for all types already having some cached
     * expression trees.)
     *
     * Assumptions:
     *     1) The handler will be resolved from the container every time the
     *        event is raised. (Depending on the handler's container
     *        registration, a different handler instance might handle the event
     *        each time its raised by the same raiser.) This essentially set
     *        up weak references to the handling methods, so long-lived
     *        raisers don't prevent short-lived handlers from being garbage
     *        collected.
     *     2) Event delegates must be of type Action<T> where T : EventBase.
     *        This is codified by GetAllActionEvents. It's possible to change
     *        this to allow arbitrary delegate signatures.
     *     3) Events are uniquely identifiable from their Action<> type's
     *        type parameter. In other words, subscriptions cannot differentiate
     *        on event name, just event type. Shouldn't be too hard to support
     *        this... allow greater event specification in the "Event()" fluent
     *        interface method, pull that into an extended EventInfo class, and
     *        filter against it in the outer loop of CompileSetupActions.
     *     4) Event handlers must catch/handle all exceptions.
     *
     * Inspired by <http://mikehadlow.blogspot.com/2010/01/10-advanced-windsor-tricks-7-how-to.html>.
     */
    public partial class EventWiringFacility : IFacility {
        private const string _SUBSCRIPTION_PROPERTY_KEY = "EventSubscription";
        private static readonly Type _FACILITY_TYPE = typeof (EventWiringFacility);

        private bool _isFrozen;
        private IKernel _kernel;

        private readonly IDictionary<Type, ICollection<EventWiringFacilityHandlerInfo>> _subscriptionConfigs =
            new Dictionary<Type, ICollection<EventWiringFacilityHandlerInfo>>()
        ;
        private readonly IDictionary<Type, IEnumerable<Action<object>>> _setupActionsByComponent =
            new Dictionary<Type, IEnumerable<Action<object>>>()
        ;

        public void Init(IKernel kernel, IConfiguration facilityConfig) {
            _kernel = kernel;
            kernel.ComponentModelCreated += KernelOnComponentModelCreated;
            kernel.ComponentCreated += KernelOnComponentCreated;
        }

        private void KernelOnComponentModelCreated(ComponentModel model) {
            var subscriptionKeys = model.ExtendedProperties.Keys.
                Cast<string>().
                Where(k => k.StartsWith(_SUBSCRIPTION_PROPERTY_KEY))
            ;

            foreach (var key in subscriptionKeys) {
                if (_isFrozen) throw new InvalidOperationException("A Component was registered after the EventWiringFacility was frozen. Late registrations are not allowed, to ensure that all subscribers will receive all event notifications.");

                var subscriptionConfig = (Subscription) model.ExtendedProperties[key];
                if (!_subscriptionConfigs.ContainsKey(subscriptionConfig.EventType)) {
                    _subscriptionConfigs.Add(subscriptionConfig.EventType, new List<EventWiringFacilityHandlerInfo>());
                }

                var info = new EventWiringFacilityHandlerInfo(model.Name, model.Services.Single(), subscriptionConfig.Handler);
                _subscriptionConfigs[subscriptionConfig.EventType].Add(info);
            }
        }

        private void KernelOnComponentCreated(ComponentModel model, object instance) {
            IEnumerable<Action<object>> setupActions;
            // The basic idea here is that we need type-specific custom logic to hook up event handlers.
            // In order to avoid calls to DynamicInvoke (poor performance), we dynamically construct
            // the type-specific logic with reflection and expression trees, compile it, and cache it.
            if (!_setupActionsByComponent.TryGetValue(model.Implementation, out setupActions)) {
                var componentEvents = GetAllActionEvents(model);
                _setupActionsByComponent[model.Implementation] = setupActions = CompileSetupActions(componentEvents, model.Implementation);
            }

            foreach(var setupAction in setupActions) {
                /* Basically:
                 *
                 * void setupAction<TComponent>(object instance) {
                 *   TComponent component = (TComponent)instance;
                 *
                 *   // Closure-bound variables.
                 *   string handlerComponentId = "whatever";
                 *   Action<THandler, TEventData> invoker = (h, e) => h.HandleMethod(e);
                 *   Action<TEventData> lazyInvoker = eventData => this.LazyRunner( handlerComponentId, invoker, eventData );
                 *
                 *   component.Add_Executed(lazyInvoker);
                 * }
                 */
                setupAction(instance);
            }
        }

        private IEnumerable<Action<object>> CompileSetupActions(IEnumerable<EventWiringFacilityEventInfo> componentEvents, Type componentImplementation) {
            var setupActions = new List<Action<object>>();
            foreach (var eventMeta in componentEvents) {
                var /*Action<object, Action<TEvent>>*/ addHandlerAction = (Delegate)(_FACILITY_TYPE.
                    GetMethod("GetAddHandlerAction", BindingFlags.Instance | BindingFlags.NonPublic).
                    MakeGenericMethod(componentImplementation, eventMeta.EventType).
                    Invoke(this, new[] {eventMeta.AddHandler})
                );

                var eventSetupActions = new List<Delegate /* Action<"eventMeta.EventType"> */>();

                foreach (var subscription in _subscriptionConfigs[eventMeta.EventType]) {
                    var /*Action<THandler, TEvent>*/ invoker = (Delegate)(_FACILITY_TYPE.
                        GetMethod("CreateInvoker", BindingFlags.Static | BindingFlags.NonPublic).
                        MakeGenericMethod(subscription.Handler.ReflectedType, eventMeta.EventType).
                        Invoke(null, new object[]{subscription.Handler})
                    );

                    var /*Action<TEvent>*/ lazyInvoker = (Delegate)(_FACILITY_TYPE.
                        GetMethod("CreateLazyInvoker", BindingFlags.Instance | BindingFlags.NonPublic).
                        MakeGenericMethod(subscription.ServiceType, eventMeta.EventType).
                        Invoke(this, new object[]{subscription.ComponentId, invoker})
                    );

                    var /*Action<THandler>*/ eventSetupAction = (Delegate)(_FACILITY_TYPE.
                        GetMethod("CreateSetupAction", BindingFlags.Static | BindingFlags.NonPublic).
                        MakeGenericMethod(componentImplementation, eventMeta.EventType).
                        Invoke(null, new [] {addHandlerAction, lazyInvoker})
                    );

                    eventSetupActions.Add(eventSetupAction);
                }

                var setupAction = (Action<object>)(_FACILITY_TYPE.
                    GetMethod("CreateAggregateSetupAction", BindingFlags.Static | BindingFlags.NonPublic).
                    MakeGenericMethod(componentImplementation).
                    Invoke(null, new []{ eventSetupActions })
                );

                setupActions.Add(setupAction);
            }
            return setupActions;
        }

        // The point of this is to cast all untyped delegates back to a strong type and close over those references,
        // and to then invoke them with the same casted THandler instance. This way we need only perform one cast
        // per component creation.
        private static Action<object> CreateAggregateSetupAction<THandler>(IEnumerable<Delegate> eventSetupActions) {
            private var typedSetupActions = eventSetupActions.Cast<Action<THandler>>().ToList();

            // This will be invoked in KernelOnComponentCreated.
            return obj => {
                private var handler = (THandler) obj;
                foreach (private var e in typedSetupActions) private e(handler);
            };
        }

        // Invoked via reflection.
        private static Delegate CreateInvoker<THandler, TEvent>(MethodInfo method) {
            var handlerParamX = Expression.Parameter(typeof(THandler));
            var eventParamX = Expression.Parameter(typeof(TEvent));
            var callX = Expression.Call(handlerParamX, method, eventParamX);
            var lambda = Expression.Lambda(callX, handlerParamX, eventParamX);
            return lambda.Compile();
        }

        // Invoked via reflection.
        private static Action<THandler> CreateSetupAction<THandler, TEvent>(Delegate addHandlerDelegate, Delegate lazyInvokerDelegate) {
            // We cast them strongly, then close over the casted references.
            var addHandler = (Action<THandler, Action<TEvent>>) addHandlerDelegate;
            var lazyInvoker = (Action<TEvent>) lazyInvokerDelegate;

            // This will be invoked by the delegate returned by CreateAggregateSetupAction.
            return handler => addHandler(handler, lazyInvoker);
        }

        // Invoked via reflection.
        private void LazyRunner<THandler, TEvent>(string handlerComponentId, Action<THandler, TEvent> invoker, TEvent arg) {
            var handler = _kernel.Resolve<THandler>(handlerComponentId);
            try {
                invoker(handler, arg);
            } finally {
                _kernel.ReleaseComponent(handler);
            }
        }

        // Invoked via reflection.
        private Delegate CreateLazyInvoker<THandler, TEvent>(string handlerComponentId, Action<THandler,TEvent> invoker) {
            // Builds this:
            // (TEvent arg) => this.LazyRunner(handlerComponentId, invoker, arg);

            var lazyRunnerMethod = _FACILITY_TYPE.
                GetMethod("LazyRunner", BindingFlags.Instance | BindingFlags.NonPublic).
                MakeGenericMethod(typeof (THandler), typeof (TEvent))
            ;

            var thisConstX = Expression.Constant(this);
            var eventArgParamX = Expression.Parameter(typeof (TEvent));
            var handlerComponentIdConstX = Expression.Constant(handlerComponentId);
            var invokerConstX = Expression.Constant(invoker);
            var callX = Expression.Call(thisConstX, lazyRunnerMethod, handlerComponentIdConstX, invokerConstX, eventArgParamX);
            var lambdaX = Expression.Lambda(callX, eventArgParamX);
            return lambdaX.Compile();
        }

        // Invoked via reflection.
        private Delegate GetAddHandlerAction<TRaiser, TActionEvent>(MethodInfo addHandlerMethod) {
            // This returns a delegate that is invoked in delegate returned by CreateSetupAction and looks like:
            // (TRaiser raiser, TActionEvent handler) => raiser.Add_Raised(handler);
            var raiserX = Expression.Parameter(typeof(TRaiser));
            var handlerParamX = Expression.Parameter(typeof (Action<TActionEvent>));
            var callX = Expression.Call(raiserX, addHandlerMethod, handlerParamX);
            var lambdaX = Expression.Lambda(callX, raiserX,handlerParamX);
            return lambdaX.Compile();
        }

        private static IEnumerable<EventWiringFacilityEventInfo> GetAllActionEvents(ComponentModel model) {
            var coll = new List<EventWiringFacilityEventInfo>();

            // Find all potential event property add methods (these are the methods that the compiler
            // generates for us when we use the 'event' keyword). Need to use this technique instead
            // of directly adding each handler to eventInfo with AddEventHandler bc .NET 4.0 multicast
            // delegate contravariance is broken.
            var eventAddHandlerMethodCandidates = model.Implementation.
                GetMethods(BindingFlags.Public | BindingFlags.Instance).
                Where(mi => mi.Name.StartsWith("add_") || mi.Name.Contains(".add_")).
                Where(mi => mi.GetParameters().Count() == 1)
            ;

            foreach (var method in eventAddHandlerMethodCandidates) {
                var paramType = method.GetParameters().Single().ParameterType;
                if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof (Action<>)) continue;

                coll.Add(new EventWiringFacilityEventInfo(method, paramType.GetGenericArguments().Single()));
            }
            return coll;
        }

        public void Freeze() { _isFrozen = true; }
        public void Terminate() {}

        public static string CreateExtendedPropertyKey(Type @event, MethodInfo handler) {
            return string.Format("{0}|{1}|{2}|{3}", _SUBSCRIPTION_PROPERTY_KEY, @event.FullName, handler.ReflectedType, handler.Name);
        }
    }
}