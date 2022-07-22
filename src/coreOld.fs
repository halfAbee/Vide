
module LocSta

open System

// TODO: struct tuples

type State<'s> = { typeChain: string; value: 's }
type Gen<'v,'s,'r> = Gen of ('s option -> 'r -> ('v * 's))
type NoState = NoState

type Type with
    member this.DisplayName =
        let rec getTypeName (t: Type) =
            match t.GenericTypeArguments with
            | [| |] -> t.Name
            | args ->
                let args = args |> Array.map getTypeName |> String.concat ", "
                $"{t.Name}<{args}>"
        getTypeName this

module State =
    let create typeChain value =  { typeChain = typeChain; value = value }
    let none = create "NoState" NoState

module Gen =
    let run (Gen g) = g

    /// Bind with transparent (nested) state typing.
    let bind 
        (Gen m: Gen<'v1, State<'s1>, 'r>)
        (f: 'v1 -> Gen<'v2, State<'s2>, 'r>)
        : Gen<'v2, State<State<'s1> * State<'s2>>, 'r>
        =
        fun mfState r ->
            // unpack the previous state (may be None or Some)
            let mState,fState =
                match mfState with
                | None -> None,None
                | Some { value = mState,fState } -> Some mState, Some fState
            let mOut,mState' = m mState r
            let (Gen fgen) = f mOut
            let fOut,fState' = fgen fState r
            let newState =
                let newStateValue = mState', fState'
                let typeChain = mState'.typeChain + "  ::  " + fState'.typeChain
                State.create typeChain newStateValue
            fOut, newState
        |> Gen

    let ofValue x =
        Gen <| fun s r -> x, State.none

    let read<'r> : Gen<'r,_,'r> =
        Gen <| fun s r -> r, State.none

    let statesAndValue (Gen g) =
        Gen <| fun s r ->
            let v,s' = g s r
            {| inState = s; value = v; outState = s' |}, s'

    // TODO: Some of them should not be auto-opened
    let map proj (Gen g) = 
        Gen <| fun s r -> let o,s = g s r in proj o, s

    let inline preserve (factory: unit -> 's) = 
        printfn $"Gen.preserve: Type = {typeof<'s>}"
        Gen <| fun s r ->
            let state = s |> Option.defaultWith (fun () ->
                let stateValue = factory()
                State.create typeof<'s>.DisplayName stateValue)
            state.value, state

    let inline ofMutable (initialValue: 'a) =
        printfn $"Gen.ofMutable: Type = {typeof<'a>}"
        Gen <| fun s r ->
            let state = s |> Option.defaultWith (fun () ->
                let stateValue = ref initialValue
                State.create typeof<'a>.DisplayName stateValue)
            let refCell = state.value
            let setter = fun value -> refCell.contents <- value
            (refCell.contents, setter), state

    let toEvaluable (Gen g: Gen<_,State<'s>,_>) =
        let mutable state = None
        fun r ->
            let fOut,fState = g state r
            state <- Some fState
            printfn $"State Chain after eval: \n\n{fState.typeChain}"
            fOut

    type GenBuilder() =
        member _.Bind(m, f) = bind m f
        member _.Return(x) = ofValue x
        member _.ReturnFrom(x) : Gen<_,_,_> = x

let gen = Gen.GenBuilder()
