namespace CheckProj

open System;

module Program =

    [<EntryPoint>]
    let main argv = 
        let result = 
            try
                Application.Run argv
            with | :? ArgumentException as ex -> printfn "Error: %s" (ex.Message); 2;

        printfn "result: %i" result
        Console.ReadLine() |> ignore
        result // return an integer exit code
