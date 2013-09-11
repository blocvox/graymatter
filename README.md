ScrambledBrains.EventWiring
===========================
High-performance, resolution-time wiring of event raisers and handlers in Windsor-managed applications.

Quick Start
-----------
Given event raiser and event handler of

    class Publisher {
        public event Action<InterestingEventData> OnInterestingEvent;
    }

    class Subscriber {
        public void HandleInterestingEventA(InterestingEventData args) { /* ... */ }
        public void HandleInterestingEventB(InterestingEventData args) { /* ... */ }
    }

they can be wired up with

    container.AddFacility(new EventWiringFacility());
    container.Register(
        Component.For<Subscriber>().
    
        // Method 1: Strongly-typed for hand-coding.
        SubscribesTo().
        Event<InterestingEventData>().
        With((subscriber, arg) => subscriber.HandleInterestingEventA(arg)).
    
        // Method 2: string-based for metaprogramming.
        SubscribesToEvent(typeof(InterestingEventData), "HandleInterestingEventB")
    );


About
-----
In C#, `event` provides a language-level implementation of the Observer Pattern. Despite some limitations, it remains the idiomatic approach to decoupled reactive/event-driven programming.  **EventWiringFacility** carries this idiom to the Castle Windsor IoC container.

The solution contains two projects: the facility library, and a sample application.

Several things make EventWiringFacility nice:
 - subscriber-oriented for looser coupling, unlike the publisher-oriented, tighter-coupled facility provided with Windsor,
 - fluent API that supports compile-time symbol checking,
 - metaprogramming-friendly,
 - event raisers do not hold direct references to event handlers, reducing risk of memory leaks and reducing depth of resolved object graphs, and
 - event wiring logic is dynamically constructed, compiled and cached for performance.

EventWiringFacility also imposes some conventions:
 - event signatures must conform to `Action<>`,
 - events are identified by their signature, not by name (when an event handler subscribes to an event signature that is matched by multiple events on a type, all such events will be wired to the handler), and
 - the facility supports a Frozen state, during which new event subscriptions are not allowed.

Known Issues
------------
 - No automated tests (though there are some Debug assertions).
 - No Nuget, neither for the Castle dependency nor for distribution.
 - Source terminology shifts between "raisers/handlers" and "publishers/subscribers".

License
-------
Copyright 2013 Michael McGranahan.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.