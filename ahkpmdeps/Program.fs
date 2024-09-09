namespace ahkpmdeps
//Op: Auto
open FSharp.SystemCommandLine
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.Json
//Op: End
[<AutoOpen>]
module Abbreviations =
    type DI = DirectoryInfo
    type FI = FileInfo

module PackageFileModel =
    //let SamplePackageFile =
    //    """
    //    {
    //      "version": "1.0.0",
    //      "description": "",
    //      "repository": "https://github.com/pstaszko/AHK_Vanilla",
    //      "website": "https://github.com/pstaszko/AHK_Vanilla",
    //      "license": "MIT",
    //      "issueTracker": "https://github.com/pstaszko/AHK_Vanilla/issues",
    //      "include": "Vanilla.ahk",
    //      "author": {
    //        "name": "Paul Staszko",
    //        "email": "paulstaszko@gmail.com",
    //        "website": ""
    //      },
    //      "scripts": {},
    //      "dependencies": {
    //        "github.com/pstaszko/AHK-Notification": "branch:master",
    //        "github.com/pstaszko/AHK_StringManipulation": "branch:main",
    //        "github.com/pstaszko/AHK_Vanilla_FileSystem": "branch:main"
    //      }
    //    }
    //    """

    type Author =
        {
            name: string
            email: string
            website: string
        }
    //type Dependency =
    //    {
    //        name: string
    //        version: string
    //    }
    type PackageFile =
        {
            version: string
            description: string
            repository: string
            website: string
            author: Author
            license: string
            issueTracker: string
            ``include``: string
            dependencies: IDictionary<string, string>
        }

    let cleanURL (value: string) = 
        value.Replace("github.com/", "").Replace("https://", "")

    let TryParsePackageContent (content: string) =
        try
            JsonSerializer.Deserialize<PackageFile>(content)
            |> fun x ->
                { x with
                    repository = cleanURL x.repository
                    dependencies =
                        x.dependencies
                        |> Seq.map (fun kvp -> cleanURL kvp.Key, cleanURL kvp.Value)
                        |> dict
                        |> fun x -> x
                }
            |> Ok
        with ex -> ex.Message |> Error

module LockFileModel =
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

    let TryParseLockFileContent (content: string) =
        try
            JsonSerializer.Deserialize<AHKPMLockFile>(content)
            |> Ok
        with ex -> ex.Message |> Error

module FSharp_SystemCommandLine_Utils =
    let dirOrCur =
        let currentDir = new System.Func<DI>(fun () -> System.IO.Directory.GetCurrentDirectory() |> DI)
        Input.OfArgument(System.CommandLine.Argument<DirectoryInfo>("Directory", currentDir, "The directory, or current directory if left blank"))

module IO =
    let getFileHere (dir: DI) name =
        let path = Path.Combine(dir.FullName, name)
        if System.IO.Path.Exists path then
            System.IO.File.ReadAllText path
            |> Ok
        else
            Error $"File is missing {path}"

module Git =
    open LibGit2Sharp
    let cleanSha (sha: string) = sha.Substring(0, 7)

    let private GetHeadSha (dir: DirectoryInfo) =
        new Repository(dir.FullName)
        |> fun x -> x.Head.Tip.Sha

    let rec TryGetHeadShaOrLookInparentRecursively (dir: DirectoryInfo) =
        try
            GetHeadSha dir |> Ok
        with
        | ex ->
            if dir.Parent = null then
                Error ex.Message
            else
                TryGetHeadShaOrLookInparentRecursively dir.Parent

module Core =
    open Git
    open IO
    open PackageFileModel

    let combineResults (result1: Result<'a, 'e>) (result2: Result<'b, 'e>) : Result<'a * 'b, 'e list> =
        match result1, result2 with
        | Ok v1, Ok v2 -> Ok (v1, v2)
        | Ok _, Error e -> Error [e]
        | Error e1, Ok _ -> Error [e1]
        | Error e1, Error e2 -> Error [e1; e2]
    
    let GenerateDotSyntax (dir: DirectoryInfo) =
        //let x = new LibGit2Sharp.Repository(dir.FullName)
        //let sha = x.Head.Tip.Sha
        TryGetHeadShaOrLookInparentRecursively dir
        |> Result.mapError List.singleton
        |> Result.bind (fun sha ->
            let packageFile =
                getFileHere dir "ahkpm.json"
                |> Result.bind PackageFileModel.TryParsePackageContent
            let lockFile =
                getFileHere dir "ahkpm.lock"
                |> Result.bind LockFileModel.TryParseLockFileContent
            //let findShaOfDependency (lock: LockFileModel.AHKPMLockFile) (dep: string) =
            //    lock.resolved
            //    |> Seq.tryFind (fun x -> x.name = dep)
            //    |> Option.map (fun x -> x.sha)
            //    |> Option.defaultValue ""
            combineResults packageFile lockFile
            |> Result.lift
                (fun (package, lock) ->
                    let shaOfDependency = 
                        lock.resolved 
                        |> Seq.map 
                            (fun x -> x.name |> cleanURL, x.sha |> cleanSha) 
                        |> Map.ofSeq
                    package.dependencies.Keys
                    |> Seq.map (fun dep -> $"\"{package.repository} {sha |> cleanSha}\" -> \"{dep} {shaOfDependency |> Map.find dep}\"")
                    |> Seq.toList
                )
            |> fun x -> x
        )

module Commands =
    open Core
    let OutputDotSyntax (dir: DirectoryInfo) =
        GenerateDotSyntax dir
        |> function
        | Ok x ->
            x |> String.concat "\n" |> printfn "%s"
            0
        | Error e ->
            e |> String.concat "\n" |> printfn "%s"
            1

module Main =
    open FSharp_SystemCommandLine_Utils

    [<EntryPoint>]
    let main args =
        rootCommand args {
            description "ahkpmdeps"
            setHandler id
            addCommand (
                command "OutputDotSyntax" {
                    description "Output Dot syntax for specified directory"
                    inputs dirOrCur
                    setHandler (Commands.OutputDotSyntax)
                })
        }