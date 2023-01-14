[<AutoOpen>]
module Vide.Fable

open System.Runtime.CompilerServices
open Browser
open Browser.Types
open Vide
open System

type FableContext
    (
        parent: Node, 
        evaluateView: FableContext -> unit
    ) =
    inherit VideContext()
    let mutable keptChildren = []
    let keepChild child = keptChildren <- (child :> Node) :: keptChildren
    let appendToParent child = parent.appendChild(child) |> ignore
    override this.RequestEvaluation() = evaluateView this
    member internal _.EvaluateView = evaluateView
    member _.Parent = parent
    member _.AddElement<'n when 'n :> HTMLElement>(tagName: string) =
        let elem = document.createElement tagName 
        do elem |> keepChild 
        do elem |> appendToParent 
        elem :?> 'n
    member _.AddText(text: string) =
        let elem = document.createTextNode text 
        do elem |> keepChild
        do elem |> appendToParent
        elem
    member _.KeepChild(child: Node) =
        child |> keepChild |> ignore
    member _.GetObsoleteChildren() =
        let childNodes =
            let nodes = parent.childNodes
            [ for i in 0 .. nodes.length-1 do nodes.Item i ]
        childNodes |> List.except keptChildren

type Modifier<'n> = 'n -> unit
type NodeBuilderState<'n,'s> = option<'n> * option<'s>
type ChildAction = Keep | DiscardAndCreateNew

type NodeBuilder<'n when 'n :> Node>
    (
        createNode: FableContext -> 'n,
        checkOrUpdateNode: 'n -> ChildAction
    ) =
    inherit VideBuilder()
    // TODO: Think about storing those in context (because context is per-instance)
    member val Modifiers: Modifier<'n> list = [] with get,set
    member val InitOnlyModifiers: Modifier<'n> list = [] with get,set
    member this.Run
        (Vide childVide: Vide<'v,'fs,FableContext>)
        : Vide<'v, NodeBuilderState<'n,'fs>, FableContext>
        =
        Vide <| fun s (ctx: FableContext) ->
            Debug.print 0 "RUN:NodeBuilder"
            let inline runModifiers modifiers node =
                for m in modifiers do m node
            let s,cs = separateStatePair s
            let node,cs =
                // Can it happen that s is Some and cs is None? I don't think so.
                // But: See comment in definition of: Vide.Core.Vide
                match s with
                | None ->
                    let newNode,s = createNode ctx,cs
                    do runModifiers this.InitOnlyModifiers newNode
                    newNode,s
                | Some node ->
                    match checkOrUpdateNode node with
                    | Keep ->
                        ctx.KeepChild(node)
                        node,cs
                    | DiscardAndCreateNew ->
                        createNode ctx,None
            do runModifiers this.Modifiers node
            let childCtx = FableContext(node, ctx.EvaluateView)
            let cv,cs = childVide cs childCtx
            for x in childCtx.GetObsoleteChildren() do
                node.removeChild(x) |> ignore
                // we don'tneed this? Weak enough?
                // events.RemoveListener(node)
            cv, Some (Some node, cs)

[<Extension>]
type NodeBuilderExtensions =
    /// Called on every Vide evaluatiopn cycle.
    [<Extension>]
    static member OnEval(this: #NodeBuilder<_>, m: Modifier<_>) =
        do this.Modifiers <- m :: this.Modifiers
        this

    /// Called once on initialization.
    [<Extension>]
    static member OnInit(this: #NodeBuilder<_>, m: Modifier<_>) =
        do this.InitOnlyModifiers <- m :: this.InitOnlyModifiers
        this

type HTMLElementBuilder<'n when 'n :> HTMLElement>(elemName: string) =
    inherit NodeBuilder<'n>(
        (fun ctx -> ctx.AddElement<'n>(elemName)),
        (fun node ->
            match node.nodeName.Equals(elemName, StringComparison.OrdinalIgnoreCase) with
            | true -> Keep
            | false ->
                // TODO:
                console.log($"TODO: if/else detection? Expected node name: {elemName}, but was: {node.nodeName}")
                DiscardAndCreateNew
        )
    )

let inline text text =
    Vide <| fun s (ctx: FableContext) ->
        let createNode () = ctx.AddText(text)
        Debug.print 0 "RUN:TextBuilder"
        let node =
            match s with
            | None -> createNode ()
            | Some (node: Text) ->
                if typeof<Text>.IsInstanceOfType(node) then
                    if node.textContent <> text then
                        node.textContent <- text
                    ctx.KeepChild(node)
                    node
                else
                    createNode ()
        (), Some node

type BuilderOperations = | Clear

type VideBuilder with

    /// This allows constructs like:
    ///     div
    /// What is already allowed is (because of Run):
    ///     div { nothing }
    member _.Yield
        (nb: NodeBuilder<'n>)
        : Vide<unit, NodeBuilderState<'n,unit>, FableContext>
        =
        Debug.print 0 "YIELD NodeBuilder"
        //nb { HtmlBase.nothing }
        nb.Run(nb.Zero())
    member _.Yield
        (s: string)
        : Vide<unit,Text,FableContext>
        =
        Debug.print 0 "YIELD string"
        text s
    
    member _.Yield
        (op: BuilderOperations) 
        : Vide<unit,unit,FableContext>
        =
        Vide <| fun s ctx ->
            match op with
            | Clear -> ctx.Parent.textContent <- ""
            (),None

module App =
    let inline doCreate appCtor (host: #Node) (content: Vide<'v,'s,FableContext>) onEvaluated =
        let content = NodeBuilder((fun _ -> host), fun _ -> Keep) { content }
        let ctxCtor = fun eval -> FableContext(host, eval)
        appCtor content ctxCtor onEvaluated
    let createFable host content onEvaluated =
        doCreate App.create host content onEvaluated
    let createFableWithObjState host content onEvaluated =
        doCreate App.createWithObjState host content onEvaluated
