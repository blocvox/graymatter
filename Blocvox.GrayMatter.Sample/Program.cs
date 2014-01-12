using System.Diagnostics;
using Blocvox.GrayMatter.Facility;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Blocvox.GrayMatter.Sample {
    internal class Program {
        private static void Main() {
            // Arrange.
            var container = SetupContainer();

            // Act.
            container.Resolve<Provider>().DoSomething();

            // Assert.
            Debug.Assert(Listener.WasAInvoked);
            Debug.Assert(Listener.WasBInvoked);
            Debug.Assert(Listener.WasCInvoked);

            try { container.Resolve<UninterestingProvider>(); }
            catch { Debug.Fail("Couldn't resolve service having an event with no listeners."); }
        }

        private static WindsorContainer SetupContainer() {
            var container = new WindsorContainer();
            container.AddFacility<GrayMatterFacility>();
            container.Register(Component.For<Provider>());

            container.Register(
                Component.For<Listener>().

                // Generic method. Strongly-typed to take advantage of IntelliSense.
                ListensTo().
                Event<SomethingOccurrence>().
                With((listener, arg) => listener.HandleSomethingOccurrenceA(arg)).

                // Reflective type passing and magic string. Easiest to metaprogram.
                ListensToEvent(typeof(SomethingOccurrence), "HandleSomethingOccurrenceB").

                // Useful when metaprogramming and you already have the MethodInfo.
                ListensToEvent(
                    typeof(SomethingOccurrence),
                    typeof(Listener).GetMethod("HandleSomethingOccurrenceC")
                )
            );

            // This type offers an event, but nothing is interested in listening to it.
            container.Register(Component.For<UninterestingProvider>());

            return container;
        }
    }
}