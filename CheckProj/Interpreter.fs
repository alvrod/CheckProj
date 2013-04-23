namespace CheckProj 

open InstructionParser;
open Project
open System.Linq;
open System.Collections.Generic;

module Interpreter =

    let EnumerateReferences srcPath pattern =
        let (projects, where) = Project.EnumerateProjects (srcPath, pattern)
        if not (projects.Any()) then 
            printfn "no projects found, please specify a root path with -src under which projects can be found"
            Seq.empty
        else 
            printfn "found projects under %s" where
            seq { 
                for p in projects do
                for r in p.References do
                yield r
            }
    let ApplyRule (rule:RuleInstruction, reference:Reference) =
        printfn "applying rule"        

    let Interpret (instructions:seq<Instruction>, srcPath:string option) = 
        let referenceQueue = new Queue<Reference>()
        let instructionQueue = new Queue<Instruction>(collection = instructions)
        while instructionQueue.Any() do
            let instruction = instructionQueue.Dequeue
            match instruction() with
            | Check(check) ->
                Seq.iter (fun ref -> referenceQueue.Enqueue ref) (EnumerateReferences srcPath check.Pattern)
            | Rule(rule) ->
                query {
                    for reference in referenceQueue do
                    select (ApplyRule (rule, reference))
                } |> ignore
            | Include(inc) ->
                printfn "hola"

        0

