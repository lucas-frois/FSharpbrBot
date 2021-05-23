open System
open System.IO
open System.Net
open Funogram.Api
open Funogram.Types
open Funogram.Telegram.Api
open Funogram.Telegram.Types
open Funogram.Telegram.Bot

let [<Literal>] TokenFileName = "token"
let telegramGroupId = -1001444616437L
let webSocketEndpoint = Some "https://fsharpbrbot.herokuapp.com"

let processMessageBuild config =
  let updateArrived ctx =

    let processResultWithValue (result: Result<'a, ApiResponseError>) =
        match result with
        | Ok v -> Some v
        | Error e ->
          printfn "Server error: %A" e.Description
          None

    let processResult (result: Result<'a, ApiResponseError>) =
        processResultWithValue result |> ignore

    let botResult data = api config data |> Async.RunSynchronously
    let bot data = botResult data |> processResult

    let sendSimpleMessage text = (sendMessageBase (ChatId.Int(telegramGroupId)) text (Some ParseMode.HTML) None None None None) |> bot

    let result () =
        processCommands ctx [
            cmd "/comandos" (fun _ -> sendSimpleMessage "Os comandos disponíveis são: /comandos, /mods, /boapergunta e /oi.")
            cmd "/mods" (fun _ -> sendSimpleMessage "ping @lucas_frois @pedrocastilho @weslenng @Lucasteles42")
            cmd "/boapergunta" (fun _ -> sendSimpleMessage "https://stackoverflow.com/help/how-to-ask")
            cmd "/oi" (fun _ -> sendSimpleMessage $"Seja bem vindo! Já programa em F#? :) {Environment.NewLine}As regras do grupo e materiais pra aprender a linguagem estão na mensagem pinada, fiquem a vontade para interagir.")
        ] |> ignore
        ()

    result()

    let newUsers () =
        match ctx.Update.Message with
        | None -> Seq.empty
        | Some x -> x.NewChatMembers |> Option.defaultValue Seq.empty

    let hasNewUsers = newUsers () |> Seq.isEmpty |> not
    let usernames = newUsers () |> List.ofSeq |> List.map (fun x -> x.Username) |> List.choose id |> List.map (fun x -> "@" + x)
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

  match webSocketEndpoint with
  | Some webSocketEndpoint ->
    async {
      let apiPath = sprintf "/%s" config.Token
      let webSocketEndpoint = sprintf "%s%s" webSocketEndpoint apiPath
      let! hook = setWebhookBase webSocketEndpoint None None None |> api config
      match hook with
      | Ok _ ->
        use listener = new HttpListener()
        listener.Prefixes.Add((sprintf "http://*:%s/" (Environment.GetEnvironmentVariable("PORT"))))
        listener.Start()

        let webhook = { Listener = listener; ValidateRequest = (fun req -> req.Url.LocalPath = apiPath) }
        return! startBot { config with WebHook = Some webhook } updateArrived None
      | Error e -> 
        printf "Can't set webhook: %A" e
        return ()
    }
  | _ ->
    async {
      let! _ = deleteWebhookBase () |> api config
      return! startBot config updateArrived None
    }

[<EntryPoint>]
let main _ =
  let startBot =
    if File.Exists(TokenFileName) then
      start (File.ReadAllText(TokenFileName))
    else
      start (Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"))
  startBot |> Async.RunSynchronously
  0