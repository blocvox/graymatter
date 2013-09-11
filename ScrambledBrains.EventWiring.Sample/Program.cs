using System.Diagnostics;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using ScrambledBrains.EventWiring.Facility;

namespace ScrambledBrains.EventWiring.Sample {
    internal class Program {
        private static void Main() {
            // Arrange.
            var container = SetupContainer();

            // Act.
            container.Resolve<Publisher>().DoSomething();

            // Assert.
            Debug.Assert(Subscriber.WasAInvoked);
            Debug.Assert(Subscriber.WasBInvoked);
            Debug.Assert(Subscriber.WasCInvoked);
        }

        private static WindsorContainer SetupContainer() {
            var container = new WindsorContainer();
            container.AddFacility(new EventWiringFacility());
            container.Register(Component.For<Publisher>());

            container.Register(
                Component.For<Subscriber>().

                // Subscribe with generic method. Strongest typing and takes advantage of symbol rename tools.
                SubscribesTo().
                Event<SomethingOccurrence>().
                With((subscriber, arg) => subscriber.HandleSomethingOccurrenceA(arg)).

                // Subscribe with reflective type passing and magic string. Easiest to metaprogram.
                SubscribesToEvent(typeof(SomethingOccurrence), "HandleSomethingOccurrenceB").

                // Useful when metaprogramming and you already have the MethodInfo.
                SubscribesToEvent(
                    typeof(SomethingOccurrence),
                    typeof(Subscriber).GetMethod("HandleSomethingOccurrenceC")
                )
            );

            return container;
        }
    }
}