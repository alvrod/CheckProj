namespace CheckProj
    
open CheckProj.Arguments;
open CheckProj.Project;
open CheckProj.InstructionParser;
open CheckProj.Interpreter;
open System;
open System.IO;
open System.Linq;

module Application = 

    let Run args =
        
         // Define what arguments are expected
         let defs = [
             {ArgInfo.Command="src"; Description="Path to source tree"; Required=false; }
             {ArgInfo.Command="rules"; Description="Path to rules file"; Required=true; }
         ]

         let parsedArgs = Arguments.ParseArgs args defs
         Arguments.DisplayArgs parsedArgs

         // convert IDictionary<T> to F# Map, explanation at http://stackoverflow.com/a/2460291/1550
         // it's just for the kicks of using the Some value below
         let argsMap = (parsedArgs :> seq<_>) |> Seq.map (|KeyValue|) |> Map.ofSeq
         
         // find & parse instructions
         let instructions = 
            ParseFile argsMap.["rules"] 
            |> Seq.filter (fun i -> i.IsSome) 
            |> Seq.map (fun i -> i.Value)

         Interpreter.Interpret (instructions, argsMap.TryFind "src")
         |> Seq.filter (fun r -> r.Result = EvaluationResult.Rejected) 
         |> Seq.map (fun r -> printf "%s" r.AppliedRule.Comment)
         |> Seq.length
        