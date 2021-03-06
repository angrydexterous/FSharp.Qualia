﻿module Dispatcher

open FSharp.Qualia
open Types
open Chessie.ErrorHandling

type T() = 
    let add (m : TodoList.Model) =
        m.Items.Add(TodoItemModel(m.NewItemText.Value, false))
        m.NewItemText.Value <- ""
        ok ()
    
    let delete (i : TodoItemModel) (m : TodoList.Model) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore
        ok ()
    
    let check (v : bool) (i : TodoItemModel) (m : TodoList.Model) = 
        tracefn "%sCHECKED %A" (if v then "" else "UN") i.Text
        i.Done.Value <- v
        m.DoneCount.Value <- m.DoneCount.Value + if v then 1 else -1
        ok ()
    
    let checkall (value : bool) (m : TodoList.Model) =
        m.Items |> Seq.iter (fun i -> i.Done.Value <- value) |> ok


    interface IDispatcher<TodoListEvents, TodoList.Model> with
        member this.InitModel m = 
            Seq.init 5 (fun i -> TodoItemModel((sprintf "Item %i" i), i % 2 = 0)) |> Seq.iter m.Items.Add

        member this.Dispatcher = 
            function 
            | Add -> add |> Sync
            | Delete item -> delete item |> Sync
            | Checked(v, item) -> check v item |> Sync
            | CheckAll -> checkall true |> Sync
            | UncheckAll -> checkall false |> Sync
            | NewItemTextChanged text -> Sync(fun m -> ok (m.NewItemText.Value <- text))
            | FilteringChanged f -> Sync(fun m -> ok (m.FilteringType.Value <- f))
            | SelectionChanged item -> Sync(fun m -> ok (m.SelectedItem.Value <- item))