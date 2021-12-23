# SwampMonster - making sense of the 'event swamp'
![Swamp Monster](docs/Swamp_Monster.png)

## Screenshots
<details>
<p/>

  <details>
    <summary>Main page</summary><p/>

  ![Main page](docs/index.png)
  </details>
  <p/>

  <details>
    <summary>Source file</summary><p/>

  ![Source file](docs/contact.png)
  </details>
   <p/>

  <details>
    <summary>Searching for an event</summary><p/>

  ![Searching for an event](docs/events-find.png)
  </details>
  <p/>

</details>

## What is an 'event swamp' aka 'event soup'?
<details>
'Event soup' is an anti-pattern in which components communicate with each other via an event bus
or similar messaging system.  As the system grows, the problem then becomes that interactions
and dependencies are non-obvious.  Further, components which receive a message may then, in turn,
generate more messages; and the sequence of messages is entirely non-obvious.

[redux](https://github.com/reduxjs/redux/issues/1266)
```text
That scenario that Flux tries to avoid is sometimes known as "event soup", and it happens a lot
in applications that rely on event buses or similar, where events get chained in unexpected ways,
sometimes get triggered multiple times without the developer realizing it, specially when
dispatches are triggered inside if clauses.
```

[Angular](https://blog.angular-university.io/angular-2-smart-components-vs-presentation-components-whats-the-difference-when-to-use-each-and-why/)
```text
This is not an accident, it's by design and probably to avoid event soup scenarios that the use
of solutions similar to a service bus like in AngularJs $scope.$emit() and $scope.$broadcast() 
tend to accidentally create.

These type of mechanisms tend to end up creating tight dependencies between different places of
the application that should not be aware of each other, also events end up being triggered
multiple times or in a sequence that is not apparent while just looking at one file.
```

This anti-pattern is called 'event soup' due to it's lack of structure and non-obvious interactions -
just like a bowl of soup!  Here we also call it an 'event swamp' - just like a bowl of soup which has
been left too long, gone rotten and contains monsters waiting to bite you!
</details>

## Prerequisites
* .NET Core 6.0 or higher

## Getting Started

### Building
```bash
$ git clone https://github.com/TrevorDArcyEvans/SwampMonster.git
$ cd SwampMonster
$ dotnet build SwampMonster.sln
```
### Running
```bash
$ cd SwampMonster.CLI/bin/Debug/net5.0/
$ ./SwampMonster.CLI.exe ../../../../EventSwamp.sln -o EventSwamp
```
* open [main page](SwampMonster.CLI/bin/Debug/net5.0/EventSwamp/index.html)

## Terminology
### Sinks
External events to which an object subscribes.  Typically, subscriptions are established in an object's constructor
or when the object gains ownership of another object.

### Sources
Events which an object raises to one or more subscribers.  The object does not know how many subscribers there are.
Further, the order in which subscribers receive the events is indeterminate.

## Notes
* events on interfaces will be sourced/sinked on every object which implements the interface.
This is at least consistent with _Find usages_ functionality in _JetBrains Rider_.

## Third Party Components
* CSharpFormat from [CodePaste.NET](https://github.com/RickStrahl/CodePaste.NET.git)

## Further Information:
<details>

* [Get started with semantic analysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis)
* [Analysing a .NET Codebase with Roslyn](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0)
* [roslyn-analysis](https://github.com/mattjhosking/roslyn-analysis.git)
* [Getting started with Roslyn code analysis](https://blog.wiseowls.co.nz/index.php/2020/05/12/walking-code-with-roslyn/)

</details>

## Further Work:
* ~~support WPF [`IEventAggregator`](https://prismlibrary.com/docs/event-aggregator.html)~~
* highlight source code where event is sinked/sourced
* generate a sink/source graph
  * could be difficult to visualise for all but the most trivial cases 
