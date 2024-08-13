namespace ahkpmdeps
//Op: Auto
open FSharp.SystemCommandLine
open System
open System.IO
open System.Text.Json
open System.Linq
//Op: End
module Models =
    type Resolved =
        {
            name: string
            version: string
            sha: string
            installPath: string
            dependencies: Map<string, string>
        }
    type AHKPMLockFile =
        {
            dependencies: Map<string, string>
            resolved: Resolved[]
        }
module Utils =
    open Models

    let JoinLines(text: #seq<string>) =
        String.Join(Environment.NewLine, text |> Seq.toArray)

    let out (o: obj) = System.Console.WriteLine(o.ToString())
    let cd = System.IO.Directory.GetCurrentDirectory()
    let getLockContent() =
        try
            System.IO.File.ReadAllText "ahkpm.lock"
            |> Ok
        with ex -> ex.Message |> Error

    let getDepDirsFromContent (content: string) =
        let lockFile = JsonSerializer.Deserialize<AHKPMLockFile>(content)

        let topLevelDependencies =
            lockFile.dependencies.Keys
            |> Seq.map (fun x -> "ahkpm-modules/" + x)
            |> Seq.toList

        let resolvedByNameAndSha =
            lockFile.resolved
            |> Array.groupBy (fun x -> {|Name = x.name.ToLower(); SHA = x.sha.ToLower()|})

        resolvedByNameAndSha
        |> Array.map fst
        |> Array.distinct
        |> Array.groupBy (fun x -> x.Name)
        |> Array.filter (fun (z, y) -> y.Length > 1)
        |> Array.map
            (fun (z, y) ->
                let shas =
                    y
                    |> Array.map (_.SHA)
                    |> String.concat ", "
                "Duplicate resolved entries for " + z + " with SHAs: " + shas
                |> Error
            )
        |> Result.collect
        |> Result.map ignore
        |> Result.mapError JoinLines
        |> Result.lift 
            (fun _ -> 
                let installPaths =
                    lockFile.resolved.ExceptBy(topLevelDependencies, (fun r -> r.installPath))
                    //|> Array.map (fun x -> x.installPath)
                    //|> Array.except topLevelDependencies
                    
                let installPaths =
                    lockFile.resolved
                    |> Array.map (fun x -> x.installPath)
                    |> Array.except topLevelDependencies

                let finalParts =
                    installPaths
                    |> Array.map (fun x -> "ahkpm-modules/" + (System.Text.RegularExpressions.Regex.Split(x, "ahkpm-modules/") |> Array.last))
                {|
                    installPaths = installPaths
                |}
            )

    getLockContent()
    |> Result.bind getDepDirsFromContent
    |> ignore

    let sync (dir: DirectoryInfo ) =
        out $"Hello World!: {dir.FullName}"
        ()

module Main =
    open Utils
    [<EntryPoint>]
    let main args =
        //let dir = Input.ArgumentMaybe<DirectoryInfo>("Directory", "The directory, or current directory if left blank")
        let cd = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
        let dirOrCurrent = Input.Argument<DirectoryInfo>("dir", cd)
        
        //let outputDir = Input.Option<DirectoryInfo>(
        //    aliases = ["-o";"--output-directory"], 
        //    defaultValue = cd, 
        //    description = "Output directory folder.")


        rootCommand args {
            description "ahkpmdeps"
            setHandler id
            addCommand (
                command "sync" {
                    description "Output file path for an attribute"
                    inputs dirOrCurrent
                    setHandler (sync)
                })
        }