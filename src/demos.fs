module Demos

open Browser
open Vide
open type Vide.Html


module SyntaxTests =
    let test1 =
        vide {
            let! count = Mutable.ofValue 0

            // TODO: Control what to emit
            let! countDivHtmlElement = 
                div {
                    text $"Count = {count.Value}"
                }
            do countDivHtmlElement.className <- "bam"

            // TODO: should also work with builder (without converting to Vide)?
            // TODO: div {()} gives ValueRestriction (generally {()} or () is problematic)
            //let! emptyDivElement = div { nothing }
            let! emptyDivElement = div
            do emptyDivElement.className <- "bam2"

            // We currently can't use () because of ValueRestriction, so use "nothing" hack.
            p {
                div
            }
            span
            nothing
        }


let helloWorld =
    vide {
        text "Hello World"
    }

let counter =
    vide {
        let! count = Mutable.ofValue 0

        div {
            text $"Count = {count.Value}"
        }

        button .onclick(fun _ -> count -= 1) { "dec" }
        button .onclick(fun _ -> count += 1) { "inc" }
    }

let conditionalAttributes =
    vide {
        let! count = Mutable.ofValue 0

        button .onclick(fun _ -> count += 1) {
            $"Hit me! Count = {count.Value}"
        }
        div .class'("the-message") {
            // TODO: hidden not as string; and that won't work
            span .hidden(if count.Value <> 5 then "true" else "false") {
                "You have the right to defend yourself!"
            }
        }
    }

let conditionalIfs =
    vide {
        let! count = Mutable.ofValue 0

        button .onclick(fun _ -> count += 1) {
            $"Hit me! Count = {count.Value}"
        }

        if count.Value = 5 || count.Value = 6 then
            let! valueString = preserve "Hello String"
            div .class'("the-message") { 
                $"You have the right to defend yourself! (string value {valueString})" 
            }
        if count.Value <> 5 then
            let! valueInt = preserve 42
            p { $"not yet ... with int value {valueInt}" }
    }

// TODO: That is not compiling (anymore; which is ok - document this)
let conditionalIfElse =
    vide {
        let! count = Mutable.ofValue 0

        button .onclick(fun _ -> count += 1) {
            $"Hit me! Count = {count.Value}"
        }

        "if-else cannot work like that"
        ////// TODO: That should not be used at all? And: That this seems to work
        ////// is only an edge case, because state has same type
        ////if count.Value = 5 then
        ////    div .class'("the-message") { 
        ////        $"You have the right to defend yourself!" 
        ////    }
        ////else
        ////    p { $"not yet ..." }
    }

let simpleFor =
    vide {
        for x in 0..5 do
            div .class'("card") { $"I'm element no. {x}" }
    }

let nextNum() = System.Random().Next(10000)

let statelessFor =
    vide {
        let! items = Mutable.ofValue []
        let add1 _ = items := items.Value @ [nextNum()]
        let add100 _ = items := items.Value @ [ for _ in 0..100 do nextNum() ]
        let removeAll _ = items :=  []

        button .onclick(add1) { "Add One" }
        button .onclick(add100) { "Add 100" }
        button .onclick(removeAll) { "Remove All" }
        
        for x in items.Value do
            div .class'("card") {
                let removeMe _ = items := items.Value |> List.except [x]
                button .onclick(removeMe) { $"Remove {x}" }
        }
    }

let statefulFor =
    vide {
        let! items = Mutable.ofValue []
        let add1 _ = items := items.Value @ [nextNum()]
        let add100 _ = items := items.Value @ [ for _ in 0..100 do nextNum() ]
        let removeAll _ = items := []

        button .onclick(add1) { "Add One" }
        button .onclick(add100) { "Add 100" }
        button .onclick(removeAll) { "Remove All" }
        
        for x in items.Value do
            div .class'("card") {
                let removeMe _ = items := items.Value |> List.except [x]
                button .onclick(removeMe) { $"Remove {x}" }

                let! count = Mutable.ofValue 0
                button .onclick(fun _ -> count -= 1) { "dec" }
                text $"{count.Value}  "
                button .onclick(fun _ -> count += 1) { "inc" }
        }
    }
