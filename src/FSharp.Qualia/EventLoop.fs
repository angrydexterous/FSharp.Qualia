﻿namespace FSharp.Qualia

open System.Reactive
open System.Reactive.Subjects
open System.Reactive.Linq
open System.Reactive.Threading
open System.Reactive.Concurrency
open System.Threading
open System

/// Event handlers are either synchronous or asynchronous
type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

/// Event dispatcher interface
type IDispatcher<'Event, 'Model> = 
    /// Inits the model - load data, ...
    abstract InitModel : 'Model -> unit
    /// Transforms an event in an event handler
    abstract Dispatcher : ('Event -> EventHandler<'Model>) with get

//type Dispatcher<'Event, 'Model> = {
//    InitModel : 'Model -> unit
//    Dispatcher : ('Event -> EventHandler<'Model>)
//}

/// The event loop itself, wiring all moving parts
type EventLoop<'Model, 'Event, 'Element>(v : View<'Event, 'Element, 'Model>, c : IDispatcher<'Event, 'Model>) = 
    /// Main event hub - all views events are routed through the hub
    let hub = new Subject<'Event>()
    let error (why, event) = tracefn "%A %A" why event
    
    do 
        let subscribe (e : IObservable<'Event>) = 
//            tracefn "COMPOSE %A" e
            e.Subscribe hub |> ignore
        v.composeViewEvent.Publish
        |> Observable.subscribe subscribe
        |> ignore

    member x.Inject e = hub.OnNext e

    /// Starts the event loop - will init the model, set its bindings, subscribe to the views event streams and handle them
    member this.Start() = this.StartWithScheduler(fun x -> x())
    member this.StartWithScheduler(f) = 
        c.InitModel v.Model
        v.SetBindings(v.Model)
        let obs = 
            Observer.Create(fun e -> 
                match c.Dispatcher e with
                | Sync eventHandler -> 
                    try 
                        f(fun () -> eventHandler v.Model)
                    with why -> error (why, e)
                | Async eventHandler -> 
                    Async.StartWithContinuations
                        (computation = eventHandler v.Model, continuation = ignore, exceptionContinuation = (fun why -> error(why,e)), 
                            cancellationContinuation = ignore))
            |> Observer.preventReentrancy
            |> hub.Subscribe
        v.MergedEventStreams |> Option.iter (fun x -> x.Subscribe hub |> ignore)
        obs
