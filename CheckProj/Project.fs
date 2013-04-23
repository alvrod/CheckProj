namespace CheckProj

open System;
open System.IO;
open System.Linq;
open System.Xml;

module Project = 

    type Reference = { 
        Name:string; 
        Path:string;
        IsProject:bool;
    }

    type ProjectInfo = {
        Name:string;
        Path:string;
        References:seq<Reference>
    }

    let EnumerateProjects (baseDir: string option, pattern: string) =
        let initial = 
            if baseDir.IsNone then Directory.GetCurrentDirectory()
            else baseDir.Value

        if not (Directory.Exists(initial)) then raise (ArgumentException(String.Format("{0} is not a directory", initial)))
        else
            let rec ProjectFinder (dir: string, pattern:string, depth: int) =
                if depth <= 6 then
                    let projects = Directory.EnumerateFileSystemEntries(dir, pattern)
                    if not (Seq.isEmpty(projects)) then 
                        let projectInfos = query {
                            for project in projects do
                            select (
                                let projDoc = XmlDocument()
                                projDoc.Load(project)
                                let nspaceMgr = XmlNamespaceManager(projDoc.NameTable)
                                nspaceMgr.AddNamespace("x", projDoc.DocumentElement.NamespaceURI)
                                let nameNode = projDoc.SelectSingleNode(@"//x:AssemblyName", nspaceMgr)
                                let referenceNodes: seq<XmlNode> = Seq.cast (projDoc.SelectNodes(@"//x:Reference", nspaceMgr))
                                let projectReferenceNodes: seq<XmlNode> = Seq.cast (projDoc.SelectNodes(@"//x:ProjectReference", nspaceMgr))

                                let GetAttributeValue (node: XmlNode, attr: string) =
                                    match node with
                                    | null -> String.Empty
                                    | n when n.Attributes.[attr] = null -> String.Empty
                                    | n -> n.Attributes.[attr].Value

                                let CreateReferences (nodes:seq<XmlNode>, isProject:bool) = query {                                    
                                        for referenceNode in nodes do
                                        select {
                                            Reference.Name = GetAttributeValue (referenceNode, "Include");
                                            Path = query {
                                                for child:XmlNode in Seq.cast referenceNode.ChildNodes do
                                                where (child.Name = "HintPath")
                                                select child.InnerText
                                                headOrDefault
                                            };
                                            IsProject = isProject
                                        }
                                 }
                                    
                                {
                                    ProjectInfo.Name = nameNode.InnerText; 
                                    Path = project; 
                                    References = Seq.concat [
                                        (CreateReferences (referenceNodes, false));
                                        (CreateReferences (projectReferenceNodes, true))
                                    ] 
                                }
                              )}
                        (projectInfos, dir)
                        
                    else ProjectFinder(Directory.GetParent(dir).FullName, pattern, depth + 1)
                else (Seq.empty, null)

            ProjectFinder (initial, pattern, 1)