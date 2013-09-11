using System;

namespace ScrambledBrains.EventWiring.Sample {
    internal class Subscriber {
        public static bool WasAInvoked { get; private set; }
        public static bool WasBInvoked { get; private set; }
        public static bool WasCInvoked { get; private set; }

        public void HandleSomethingOccurrenceA(SomethingOccurrence occurrence) {
            Console.WriteLine("Handler A invoked for something at " + occurrence.At);
            WasAInvoked = true;
        }

        public void HandleSomethingOccurrenceB(SomethingOccurrence occurrence) {
            Console.WriteLine("Handler B invoked for something at " + occurrence.At);
            WasBInvoked = true;
        }

        public void HandleSomethingOccurrenceC(SomethingOccurrence occurrence) {
            Console.WriteLine("Handler C invoked for something at " + occurrence.At);
            WasCInvoked = true;
        }
    }
}