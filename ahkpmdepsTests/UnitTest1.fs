module ahkpmdepsTests
open FSharp.Core
open Xunit
open WinMergeEquals
open System.IO
open FsUnit
open FsUnitTyped
open System

let sampleLockFile =
    """
    {
  "lockfileVersion": "1",
  "dependencies": {
    "github.com/pstaszko/TEMPAHK_MidLevelScript": "branch:main",
    "github.com/pstaszko/TEMPAHK_MidLevelScript2": "branch:main"
  },
  "resolved": [
    {
      "name": "github.com/pstaszko/TEMPAHK_MidLevelScript",
      "version": "branch:main",
      "sha": "8d21a7c234dbe35e511cce229aabf4bc710b64d3",
      "installPath": "ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript",
      "dependencies": {
        "github.com/pstaszko/TEMPAHK_LowLevelScript": "branch:main"
      }
    },
    {
      "name": "github.com/pstaszko/TEMPAHK_LowLevelScript",
      "version": "branch:main",
      "sha": "5de4c942922f1f0e7446d94a3eaa9ca0bc21b6c6",
      "installPath": "ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript/ahkpm-modules/github.com/pstaszko/TEMPAHK_LowLevelScript",
      "dependencies": {}
    },
    {
      "name": "github.com/pstaszko/TEMPAHK_MidLevelScript2",
      "version": "branch:main",
      "sha": "3a9846d3ad2119c9bda0c187b991df16ce99cfcf",
      "installPath": "ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript2",
      "dependencies": {
        "github.com/pstaszko/TEMPAHK_LowLevelScript": "branch:main"
      }
    },
    {
      "name": "github.com/pstaszko/TEMPAHK_LowLevelScript",
      "version": "branch:main",
      "sha": "5de4c942922f1f0e7446d94a3eaa9ca0bc21b6c6",
      "installPath": "ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript2/ahkpm-modules/github.com/pstaszko/TEMPAHK_LowLevelScript",
      "dependencies": {}
    }
  ]
}
    """

let wm expected actual =
    WinMergeEquals.WinMergeEquals.AreEqualWinMerge expected actual WhitespaceSimplify.None "Exp" "Act"
    
[<Fact>]
let Test2 () : unit =
    let currentDir rest = Path.Combine(Directory.GetCurrentDirectory(), "SampleFiles", rest) |> DirectoryInfo 
    let expected =
        [
            """ "StarlyCode/ahkpmdeps d083320" -> "pstaszko/TEMPAHK_LowLevelScript 5de4c94" """
            """ "StarlyCode/ahkpmdeps d083320" -> "pstaszko/TEMPAHK_MidLevelScript 8d21a7c" """
        ]
        |> Seq.map _.Trim()
        |> String.concat Environment.NewLine

    ahkpmdeps.Core.GenerateDotSyntax (currentDir "ValidWithDependencies")
    //ahkpmdeps.Core.GenerateDotSyntax (DirectoryInfo @"C:\Dev\AHK_Modules\AHK_Vanilla\")
    |> function
    | Ok lines ->
        lines
        |> String.concat Environment.NewLine
        |> shouldEqual expected
    | Error x ->
        x
        |> String.concat Environment.NewLine
        |> fun x -> Xunit.Assert.Fail(x)
    //|> shouldEqual 0
