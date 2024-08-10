// fsharplint:disable MemberNames
namespace ahkpmdeps
//Op: Auto
open System.Diagnostics
open System.IO

//open ExceptionalCode.Atomic
//open ExceptionalCode.AtomicX
//Op: End
[<AutoOpen>]
module TypeExtensions =
    type System.IO.File with
        static member WriteTextToFile text file = File.WriteAllText(file, text)
        static member ReadTextFromFile file = File.ReadAllText(file)

    type System.DateTime with
        member this.ShortDate = this.ToShortDateString()

    type System.IO.FileInfo with
        static member ReadAllLines(fi: FileInfo) = File.ReadAllLines fi.FullName

        static member SetText (fi: FileInfo) txt =
            File.WriteAllText(fi.FullName, txt)

        [<DebuggerHidden>]
        member this.FileNameWithoutExtension = Path.GetFileNameWithoutExtension this.FullName

    type System.Object with
        member this.As<'a>() =
            this
            |> box
            |> function
            | :? 'a as x -> Some x
            | _ -> None

    type FSharp.Core.Option<'data> with
        static member disregard (_: unit option) = ()

    type Microsoft.FSharp.Core.Result<'data, 'error> with
        [<DebuggerHidden>]
        static member lift (fn: 'data -> 'error) (res: Result<'data,'c>) = res |> Result.bind (fn >> Ok)

        [<DebuggerHidden>]
        static member liftQuiet (fn: 'data -> 'error) (res: Result<'data,'c>) = res |> Result.bind (fn >> Ok) |> ignore

        static member toOption : Result<'data, 'error> ->'data option =
            function
            | Ok x -> Some x
            | Error _ -> None

        static member digest (ok: 'data -> 'finalType) (err: 'err -> 'finalType) (res: Result<'data,'err>) : 'finalType =
            res
            |> function
            | Ok x -> ok x
            | Error x -> err x

        static member disregard (_: Result<unit, unit>) = ()

        static member disregardUnitList (results: Result<unit list, 'err>) =
            results
            |> function
            | Ok _ -> Ok()
            | Error err -> Error err

        static member collect (results: Result<'data, 'error> seq) =
            let folder state item =
                match state, item with
                | Ok(rs), Ok(r) -> Ok(r :: rs)
                | Ok _, Error(m2) -> Error([m2])
                | Error(m1), Error(m2) -> Error(m1 @ [m2])
                | Error(m1), Ok _ -> Error(m1)
            Seq.fold folder (Ok []) results
            |> Result.lift List.rev

        static member collectErrors (results: Result<unit, 'error> seq) =
            Result.collect results
            |> function
            | Ok (_: unit list) -> Ok ()
            | Error x -> Error x

        static member private folder (fn: 'data -> Result<'result, 'error>) (state: Result<'result list, 'error>) (item: 'data) =
            state
            |> Result.bind (fun x ->
                fn item
                |> Result.lift (fun z -> z :: x)
            )

        static member myFold fn items = Seq.fold (Result.folder fn) (Ok []) items

        static member ErrorIfListHasAny (fn: 'item list -> 'error) (items: 'item seq) : Result<unit, 'error> =
            items
            |> Seq.toList
            |> function
            | [] -> Ok()
            | x -> x |> fn |> Error

        static member ErrorIf (fn: 'item -> bool) (err: 'error) (items: 'item)  : Result<'item, 'error> =
            if fn items then Error err
            else Ok items

        static member Requires (fn: 'item -> bool) (err: 'error) (items: 'item)  : Result<'item, 'error> =
            Result.ErrorIf (fn >> not) err items

        static member RequiresSome (err: 'error) (res: Result<'item option, 'error>)  : Result<'item, 'error> =
            res
            |> function
            | Ok (Some x) -> Ok x
            | Ok None -> err |> Error
            | Error x -> Error x

        static member IsOk (i: Result<'a, 'b>) : bool =
            i
            |> function
            | Ok _ -> true
            | Error _ -> false

        static member AssertIsOk (i: Result<'a, 'b>) : unit =
            if Result.IsOk i |> not then failwith "Should have been Ok"

        static member AssertIsError msg (i: Result<'a, 'b>) : unit =
            i
            |> function
            | Ok _ -> failwith "Should have been an error"
            | Error err ->
                if err = msg then ()
                else failwith $"Should have been error: {msg}, but was: {err}"
