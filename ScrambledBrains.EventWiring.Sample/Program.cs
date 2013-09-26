﻿using System.Diagnostics;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using ScrambledBrains.EventWiring.Facility;

namespace ScrambledBrains.EventWiring.Sample {
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
        }

        private static WindsorContainer SetupContainer() {
            var container = new WindsorContainer();
            container.AddFacility(new EventWiringFacility());
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

            return container;
        }
    }
}