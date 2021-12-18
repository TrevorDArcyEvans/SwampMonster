# SwampMonster - making sense of the 'event swamp'
![Swamp Monster](docs/Swamp_Monster.png)

## What is an 'event swamp' aka 'event soup'
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

## Third Party Components
* CSharpFormat from [CodePaste.NET](https://github.com/RickStrahl/CodePaste.NET.git)

## Further Information:
* [Get started with semantic analysis](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis)
* [Analysing a .NET Codebase with Roslyn](https://dev.to/mattjhosking/analysing-a-net-codebase-with-roslyn-5cn0)
* [roslyn-analysis](https://github.com/mattjhosking/roslyn-analysis.git)
* [Getting started with Roslyn code analysis](https://blog.wiseowls.co.nz/index.php/2020/05/12/walking-code-with-roslyn/)

## Further Work:
* support WPF [`IEventAggregator`](https://prismlibrary.com/docs/event-aggregator.html)

