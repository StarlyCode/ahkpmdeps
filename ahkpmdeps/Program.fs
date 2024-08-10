[<EntryPoint>]
let main args =
    let cd = System.IO.Directory.GetCurrentDirectory()
    let getLockContent() =
        try
            System.IO.File.ReadAllText "ahkpm.lock"
            |> Ok
        with ex -> ex.Message |> Error
    getLockContent()
    |> Result.bind (fun content ->
        let d =System.Text.Json.JsonDocument.Parse(content)

        Ok ()
    )
    |> ignore
    0