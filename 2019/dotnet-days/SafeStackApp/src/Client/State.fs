﻿module Client.State

open System
open Domain
open Shared.Domain
open Elmish
open Elmish.SweetAlert
open Fable.Remoting.Client
open Fable.SimpleJson

//let init () : Model * Cmd<Msg> =
//    //{ Counter = -1; IsLoading = false }, Cmd.ofMsg LoadFromServer
//
//let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
//    match msg with
//    | Increment ->
//        let nextModel = { currentModel with Counter = currentModel.Counter + 1  }
//        nextModel, Cmd.none
//    | Decrement ->
//        let nextModel = { currentModel with Counter = currentModel.Counter - 1 }
//        nextModel, Cmd.none
//    | LoadFromServer ->
//        { currentModel with IsLoading = true }, Cmd.OfAsync.perform Server.countAPI.GetRandomCount () CountLoadedFromServer
//    | CountLoadedFromServer initialCount->
//        let nextModel = { currentModel with Counter = initialCount; IsLoading = false }
//        nextModel, Cmd.none


let init () : Model * Cmd<Msg> =
    { IsAddingNew = false; NewColumnName = ""; Columns = []; NewItemNames = [] }, Cmd.ofMsg ReloadColumnsFromServer

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | StartEditing -> { currentModel with IsAddingNew = true }, Cmd.none
    | CancelEditing -> { currentModel with IsAddingNew = false }, Cmd.none
    | TextEdited v -> { currentModel with NewColumnName = v }, Cmd.none
    | AddNewColumn ->
        let newModel = { currentModel with IsAddingNew = false; NewColumnName = "" }
        newModel, Cmd.OfAsync.either
                      Server.columnsAPI.AddColumn
                      currentModel.NewColumnName
                      (fun _ -> ReloadColumnsFromServer)
                      ErrorOccured
    | RemoveColumn name ->
        currentModel, Cmd.OfAsync.either Server.columnsAPI.RemoveColumn name (fun _ -> ReloadColumnsFromServer) ErrorOccured
    | ItemTextEdited (col,v) ->
        let texts = currentModel.NewItemNames |> List.filter (fun (c,_) -> c <> col )
        { currentModel with NewItemNames = (col,v) :: texts }, Cmd.none
    | AddItemToColumn colName ->
        let found,others = currentModel.NewItemNames |> List.partition (fun (c,_) -> c = colName)
        let itemName = found |> List.head |> snd
        { currentModel with NewItemNames = others }, Cmd.OfAsync.either
                                                         Server.columnsAPI.AddItemToColumn
                                                         (colName, itemName)
                                                         (fun _ -> ReloadColumnsFromServer)
                                                         ErrorOccured
//    | RemoveItemFromColumn (colName,item) ->
//        let newCols = Data.removeItem currentModel.Columns colName item
//        { currentModel with Columns = newCols }, Cmd.none
//    | CompleteItem (colName,item) ->
//        let foundItem = Data.getItem currentModel.Columns colName item
//        let newCols =
//            { foundItem with Status = Completed(DateTime.UtcNow) }
//            |> Data.replaceItem currentModel.Columns colName
//        { currentModel with Columns = newCols }, Cmd.none
    | ReloadColumnsFromServer ->
        currentModel, Cmd.OfAsync.either Server.columnsAPI.GetAll () ColumnsReloadedFromServer ErrorOccured
    | ColumnsReloadedFromServer cols ->
        { currentModel with Columns = cols }, Cmd.none
    | ErrorOccured e ->
        let alertMsg =
            match e with
            | :? ProxyRequestException as ex ->
                match ex.ResponseText |> Json.tryParseAs<{| error : string |}> with
                | Ok v -> v.error
                | Error _ -> ex.Message
            | _ -> e.Message
        let alert = SimpleAlert(alertMsg).Type(AlertType.Error)
        currentModel, SweetAlert.Run(alert)


