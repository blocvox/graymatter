ScrambledBrains.GrayMatter
==========================
High-performance, resolution-time wiring of event providers and listeners in Windsor-managed applications.

Quick Start
-----------
Given event provider and listener of

    class Provider {
        public event Action<InterestingEventData> OnInterestingEvent;
    }

    class Listener {
        public void HandleInterestingEventA(InterestingEventData args) { /* ... */ }
        public void HandleInterestingEventB(InterestingEventData args) { /* ... */ }
    }

they can be wired up with

    container.AddFacility(new GrayMatterFacility());
    container.Register(
        Component.For<Listener>().
    
        // Method 1: Strongly-typed for hand-coding.
        ListensTo().
        Event<InterestingEventData>().
        With((listener, arg) => listener.HandleInterestingEventA(arg)).
    
        // Method 2: string-based for metaprogramming.
        ListensToEvent(typeof(InterestingEventData), "HandleInterestingEventB")
    );


About
-----
In C#, `event` provides a language-level implementation of the Observer Pattern. Despite some limitations, it remains the idiomatic approach to decoupled reactive/event-driven programming.  **GrayMatterFacility** carries this idiom to the Castle Windsor IoC container.

The solution contains two projects: the facility library, and a sample application.

Several things make GrayMatterFacility nice:
 - listener-oriented for looser coupling, unlike the publisher-oriented, tighter-coupled facility provided with Windsor,
 - fluent API that supports compile-time symbol checking,
 - metaprogramming-friendly,
 - event providers do not hold direct references to listener instances, reducing risk of memory leaks and reducing depth of resolved object graphs, and
 - event wiring logic is dynamically constructed, compiled and cached for performance.

GrayMatterFacility also imposes some conventions:
 - event signatures must conform to `Action<>`,
 - events are identified by their signature, not by name (when listening to an event whose signature is used by multiple events on a type, the handler will be wired to all such events), and
 - the facility supports a Frozen state, during which new event wirings are not allowed.

More information: <http://scrambledbrains.net/2013/09/26/something-useful/>.

Known Issues
------------
 - No automated tests. :O
 - No Nuget, neither for the Castle dependency nor for distribution.

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
