namespace CheckProj 

open System;
open System.IO;
open System.Linq;
open System.Text.RegularExpressions;

module InstructionParser =
    
    (*
        * check [pattern]
        * (allow | reject) ((project [pattern])? (under [pattern])?)? (include [pattern] | any)? (path [pattern] | absolute)?
        * include [pattern]
    *)

    type CheckInstruction = {
        Pattern: string
    }

    type RuleType =
        | Allow
        | Reject

    type IncludeType =
        | Pattern of string
        | Any

    type PathType =
        | Pattern of string
        | Absolute

    type RuleInstruction = {
        Type: RuleType;
        ProjectPattern: string option;
        UnderPattern: string option;
        IncludePattern: IncludeType option;
        PathPattern: PathType option;
        Comment: string;
    }

    type IncludeInstruction = {
        Pattern: string
    }
    
    type Instruction =
        | Check of CheckInstruction
        | Rule of RuleInstruction
        | Include of IncludeInstruction

    // with thanks to http://stackoverflow.com/questions/3722591/pattern-matching-on-the-beginning-of-a-string-in-f
    let (|Prefix|_|) (p:string) (s:string) =
        if s.StartsWith(p) then Some(s.Substring(p.Length))
        else None
            
    let PatternRegex = "\s*\[(?<p>([^\]]+))\].*"
    let ParsePattern text =
        // capture pattern out of [pattern]. Sadly [] must be escaped hence the ugliness
        let capture = Regex.Match(text, PatternRegex)
        if not capture.Success then raise (ArgumentException(String.Format("Unable to find proper pattern in {0}. Patterns must be [pattern].", text)))
        capture.Groups.["p"].Value

    let ParseCheck text = { CheckInstruction.Pattern = ParsePattern text }

    let ParseInclude text = { IncludeInstruction.Pattern = ParsePattern text }

    let ParseRule (ruleType:RuleType, text:string) =
        let GetPattern (text:string, name:string) =
            let pos = text.IndexOf name
            match pos with
            | -1 -> None
            | x -> Some (ParsePattern (text.Substring (x + name.Length)))

        let (input, comment) = 
            let pos = text.IndexOf '#'
            match pos with
            | -1 -> (text, null)
            | x -> (text.Substring(0, x), text.Substring(x + 1))

        { 
            RuleInstruction.Type = ruleType;
            ProjectPattern = GetPattern (input, "project");
            UnderPattern = GetPattern (input, "under");
            IncludePattern = 
                let pattern = GetPattern (input, "include")
                if pattern.IsSome then Some (IncludeType.Pattern(pattern.Value))
                else if (input.Contains "any") then Some IncludeType.Any
                else None
            PathPattern = 
                let pattern = GetPattern (input, "path")
                if pattern.IsSome then Some (PathType.Pattern(pattern.Value))
                else if (input.Contains "absolute") then Some PathType.Absolute
                else None
            Comment = comment;
        }               

    let ParseLine line: Instruction option =
        match line with
        | Prefix "check" rest -> Some (Instruction.Check(ParseCheck rest))
        | Prefix "allow" rest -> Some (Instruction.Rule(ParseRule (RuleType.Allow, rest)))
        | Prefix "reject" rest -> Some (Instruction.Rule(ParseRule (RuleType.Reject, rest)))
        | Prefix "include" rest -> Some (Instruction.Include(ParseInclude rest))
        | Prefix "#" rest -> None
        | line when String.IsNullOrWhiteSpace(line) -> None 
        | line -> raise (ArgumentException(String.Format("Unrecognized rule {0}. Expected one of check, allow, reject, include, #.", line)))

    let ParseFile path =
        if (not (File.Exists(path))) then raise (ArgumentException(String.Format("{0} not found", path)))
        else Seq.map (fun line -> ParseLine line) (File.ReadLines path)