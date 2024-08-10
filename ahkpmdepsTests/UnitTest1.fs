module ahkpmdepsTests
open FSharp.Core
open Xunit
open WinMergeEquals

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
let Test1 () : unit =
    let expected = 
        """
ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript/ahkpm-modules/github.com/pstaszko/TEMPAHK_LowLevelScript
ahkpm-modules/github.com/pstaszko/TEMPAHK_MidLevelScript2/ahkpm-modules/github.com/pstaszko/TEMPAHK_LowLevelScript
        """
    ahkpmdeps.Utils.getDepDirsFromContent sampleLockFile
    |> function
    | Ok x -> 
        x.installPaths 
        |> String.concat "\n" 
        |> wm expected
    | Error x -> Assert.Fail x
