namespace CheckProj 

open InstructionParser
open Project
open System
open System.Linq
open System.Collections.Generic
open System.Text.RegularExpressions
open CheckProj

module Interpreter =
    type EvaluationResult =
        | Allowed
        | Rejected

    type Evaluation = {
        EvaluatedReference: Reference;
        AppliedRule: RuleInstruction;
        Result: EvaluationResult
    }

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
                yield (p, r)
            }

    let ApplyRule (rule:RuleInstruction, project:ProjectInfo, reference:Reference) =
        let applies (pattern:string option, data:string) =
            pattern.IsNone ||
            String.IsNullOrWhiteSpace (pattern.Value) || 
            Regex.IsMatch (pattern.Value, data)

        let appliesProjectName = applies (rule.ProjectPattern, project.Name)
        let appliesProjectPath = applies (rule.UnderPattern, project.Path)
        let applies =
            if (appliesProjectName || appliesProjectPath) then
                let appliesReferenceName = 
                    match rule.IncludePattern with
                    | None -> false
                    | Some IncludeType.Any -> true
                    | Some (IncludeType.Pattern str) -> applies (Some str, reference.Name)
                let appliesReferencePath = 
                    match rule.PathPattern with
                    | None -> false
                    | Some PathType.Absolute -> applies (Some "(A-Za-z):\\|\\|\\\\", reference.Path)
                    | Some (PathType.Pattern str) -> applies (Some str, reference.Path)
                (appliesProjectName || appliesProjectPath)
            else false

        if not applies then None
        else
            match rule.Type with
            | RuleType.Allow -> Some { EvaluatedReference = reference; AppliedRule = rule; Result = Allowed }
            | RuleType.Reject -> Some { EvaluatedReference = reference; AppliedRule = rule; Result = Rejected}

    let Interpret (instructions:seq<Instruction>, srcPath:string option) = 
        let referenceQueue = new Queue<(ProjectInfo * Reference)>()
        let instructionQueue = new Queue<Instruction>(collection = instructions)
        let resultQueue = new Queue<Evaluation>();

        while instructionQueue.Any() do
            match instructionQueue.Dequeue() with
            | Check(check) ->
                Seq.iter (fun ref -> referenceQueue.Enqueue ref) (EnumerateReferences srcPath check.Pattern)
            | Rule(rule) ->
                query {
                    for reference in referenceQueue do
                    select (ApplyRule (rule, (fst reference), (snd reference)))
                } |> Seq.iter (fun res -> if res.IsSome then resultQueue.Enqueue res.Value)
            | Include(inc) ->
                printfn "todo: include rules from another file"

        resultQueue
