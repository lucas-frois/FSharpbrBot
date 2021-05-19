open System
open Funogram.Api
open Funogram.Types
open Funogram.Telegram.Api
open Funogram.Telegram.Types
open Funogram.Telegram.Bot

let telegramGroupId = -1001444616437L

let processMessageBuild config =
  let updateArrived ctx =

    let processResultWithValue (result: Result<'a, ApiResponseError>) =
        match result with
        | Ok v -> Some v
        | _ ->
          printfn "Server error: %A" DateTime.Now
          None
    
    let processResult (result: Result<'a, ApiResponseError>) =
        processResultWithValue result |> ignore

    let botResult data = api config data |> Async.RunSynchronously
    let bot data = botResult data |> processResult
    
    let sendSimpleMessage text = (sendMessageBase (ChatId.Int(telegramGroupId)) text (Some ParseMode.Markdown) None None None None) |> bot
    
    let newUsers = ctx.Update.Message.Value.NewChatMembers |> Option.defaultValue Seq.empty
    let hasNewUsers = newUsers |> Seq.isEmpty |> not
    let usernames = newUsers |> List.ofSeq |> List.map (fun x -> x.Username) |> List.choose id |> List.map (fun x -> "@" + x)
    let namesConcatenated = (String.concat ", " usernames)

    match hasNewUsers with
    | false -> ()
    | true -> 
        match List.length usernames with
        | 1 -> sendSimpleMessage $"Seja bem vindo {usernames.Head}! Já programa em F#? :) {Environment.NewLine}As regras do grupo e materiais pra aprender a linguagem estão na mensagem pinada, fiquem a vontade para interagir."
        | _ -> sendSimpleMessage $"Sejam bem vindos {namesConcatenated}! Já programam em F#? :) {Environment.NewLine}As regras do grupo e materiais pra aprender a linguagem estão na mensagem pinada, fiquem a vontade para interagir."
  updateArrived

let start token =
  let config = { defaultConfig with Token = token }
  let updateArrived = processMessageBuild config
  async {
      return! startBot config updateArrived None
  }
  
[<EntryPoint>]
let main _ =
  let startBot = 
      start (Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"))
  startBot |> Async.RunSynchronously
  0
