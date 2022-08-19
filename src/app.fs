module App

open Browser.Dom
open Browser.Types
open Vide
open Fable.Core.JS

let mutable currentState = None

let demos : list<string * string * (HTMLElement -> unit)> = 
    let inline start demo = fun host ->
        let onEvaluated _ state = currentState <- state |> Option.map (fun s -> s :> obj)
        let videMachine = prepareStart host demo onEvaluated
        videMachine.Eval()

    [
        (
            "Hello World",
            "Just a message to the world...",
            start Demos.helloWorld
        )

        (
            "Counter",
            "The famous, one-of-a kind counter.",
            start Demos.counter
        )
        
        (
            "Conditional attributes",
            "Count to 5 and you'll get a surprise!",
            start Demos.conditionalAttributes
        )

        (
            "Conditional elements (multiple if)",
            "Count to 5 and you'll get another surprise!",
            start Demos.conditionalIfs
        )

        (
            "List of elements",
            "Just an immutable list.",
            start Demos.simpleFor
        )

        (
            "Mutable element list",
            "Add / Remove items",
            start Demos.statelessFor
        )

        (
            "List with element state",
            "TODO",
            start Demos.statefulFor
        )
    ]

let menu = document.getElementById("menu")
let demoHost = document.getElementById("demo")
for title,desc,runDemo in demos do
    let btn = document.createElement("button") :?> HTMLButtonElement
    btn.innerText <- title
    btn.addEventListener("click", fun evt ->
        let innerDemoHostId = "innerDemoHost"
        demoHost.innerHTML <-
            $"""
            <h2>{title}</h2> 
            <blockquote>{desc}</blockquote>
            <div id={innerDemoHostId}></div>
            """
        let innerDemoHost = demoHost.querySelector($"#{innerDemoHostId}") :?> HTMLElement
        runDemo innerDemoHost
    )
    menu.appendChild(btn) |> ignore

document.getElementById("logState").onclick <- fun _ ->
    let isNode elem = typeof<HTMLElement>.IsInstanceOfType(elem)
    let isArray elem = Array.isArray(elem)
    let flattenedState =
        match currentState with
        | None -> []
        | Some state ->
            let rec flattenState (state: obj) =
                [
                    if isArray state then
                        let arr = state :?> array<obj>
                        let e0 = arr[0]
                        let e1 = arr[1]
                        if isNode e0 
                            then yield (e0 :?> HTMLElement).tagName
                            else yield JSON.stringify(e0)
                        yield! flattenState e1
                    else
                        yield JSON.stringify(state)
                ]
            flattenState state
    //console.log(flattenedState |> List.toArray)
    //console.log(JSON.stringify(currentState, space = 2))
    console.log(currentState)
