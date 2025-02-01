# Blazor State Management
### Simple State Management for Blazor

## Installation
TODO: Create Nuget

## Getting Started
In your `program.cs` file make sure to add `Services.AddStateManagement()`, there are several overloads available.

*Note:* The event dispatcher takes advantage of Channels, which can have their capactity altered with a configuration variable called: `QueueCapacity` in your `appsettings.json` file. The default value is 100.

You may also extend/customize using the builder `Host.UseStateManagement()` which also must be added to `program.cs`

This should be enough get it up and running

## Using
Any component you wish to access a state must inherit from `StateComponent`, the applies all the needed state subscriptions to make it is updated.

Then all that is necessary is to `@inject` or `[Inject]` an `IState<T>` service.

If you wish to change the state you can inject `IDispatcher` this will give you the option of changing the state both synchronously and asynchronously. With calls to : `Dispatch()` and `DispatchAsync()` respectively.

You may also subscribe to state changes with a custom handler in your component by calling `base.RegisterStateChangeCallback()` in your constructor. There is an overload to execute both synchronous and asynchronous callbacks

There is a Demo Blazor app that demonstrates several of these options.
