namespace CheckProj
    
open CheckProj.Arguments;
open CheckProj.Project;
open CheckProj.InstructionParser;
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
         ParseFile argsMap.["rules"] 
         |> Seq.filter (fun i -> i.IsSome) 
         |> Seq.iter (fun i -> printfn "found %A" i) 

         // find & stream projects to check
         let (projects, where) = Project.EnumerateProjects (argsMap.TryFind "src", "*.fsproj")
         if not (projects.Any()) then printfn "no projects found, please specify a root path with -src and a pattern with -proj"
         else printfn "found projects under %s" where

         // stream references
         // determine rules to apply (configured rules, might depend on project and/or reference)

         // validate references, printing all errors
         // return number of errors (0 = success)
         0

