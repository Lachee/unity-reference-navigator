using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml;
using Microsoft.CodeAnalysis.Text;
using UnityEditor;
using System.Diagnostics;

namespace Lachee.ReferenceNavigator.Search
{
    //https://gist.github.com/DustinCampbell/32cd69d04ea1c08a16ae5c4cd21dd3a3
    //https://stackoverflow.com/questions/31861762/finding-all-references-to-a-method-with-roslyn

    /// <summary>
    /// Performs analytics to find code references to MonoScript assets.
    /// </summary>
    public static class AnalyticSearch
    {
        private static AdhocWorkspace workspace;
        static AnalyticSearch()
        {
            workspace = new AdhocWorkspace();
        }


        /// <summary>
        /// Searching State
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Enumerator is finished
            /// </summary>
            Done = 0,

            /// <summary>
            /// Enumerator is reading scripts
            /// </summary>
            ReadingFile,

            FindingModel,
            FindingRoot,
            FindingSymbol,
        }

        /// <summary>
        /// Fidns references using Roslyn by the Unity Coroutine paradigm. When Enumator returns, then a frame should be skipped.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="results"></param>
        public static IEnumerator<State> FindReferencesEnumerator(UnityEngine.Object asset, List<AnalyticResult> results)
        {
            Stopwatch w = new Stopwatch();
            w.Start();

            //Get the parts
            if (asset is MonoBehaviour monoBehaviour)
            {
                asset = MonoScript.FromMonoBehaviour(monoBehaviour);
            }
            else if (asset is ScriptableObject scriptableObject)
            {
                asset = MonoScript.FromScriptableObject(scriptableObject);
            }

            //Make sure its a monoscript
            if (!(asset is MonoScript))
                throw new ArgumentException("Supplied argument is not a MonoScript");

            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
            string scriptPath = Path.GetFullPath(UnityEditor.AssetDatabase.GetAssetPath(asset));

            //Prepare paths
            string folder = Application.dataPath + "/../";
            string projectPath = Directory.EnumerateFiles(folder, "Assembly-CSharp.csproj", SearchOption.TopDirectoryOnly).First();
            //projectPath = @"D:\Users\Lachee\Documents\Unity Projects\DistanceJam\Assembly-CSharp.csproj";

            //Prepare workspace
            workspace.ClearSolution();

            //Prepare solution
            var solId = SolutionId.CreateNewId();
            var solutionInfo = SolutionInfo.Create(solId, VersionStamp.Default);
            var solution = workspace.AddSolution(solutionInfo);

            //Prepare the project
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Assembly-CSharp", "Assembly-CSharp", "C#", projectPath);
            var project = workspace.AddProject(projectInfo);

            yield return State.ReadingFile;

            //Prepare the documents
            Document sourceDocument = null;
            XmlDocument xml = new XmlDocument();
            xml.Load(projectPath);
            var elements = xml.GetElementsByTagName("Compile");
            foreach (XmlElement element in elements)
            {
                var includePath = Path.GetFullPath(Path.GetDirectoryName(projectPath) + "/" + element.GetAttribute("Include"));

                //workspace.AddDocument(compileDocument);
                var name = Path.GetFileName(includePath);
                var fileContent = File.ReadAllText(includePath);
                var src = SourceText.From(fileContent);
                var doc = project.AddDocument(name, src, filePath: includePath);
                project = doc.Project;
                if (includePath.Equals(scriptPath)) sourceDocument = doc;
                yield return State.ReadingFile;
            }

            workspace.TryApplyChanges(solution);

            if (sourceDocument != null)
            {
                //Find the model
                var modelAwait = sourceDocument.GetSemanticModelAsync().ConfigureAwait(false);
                while (!modelAwait.GetAwaiter().IsCompleted) yield return State.FindingModel;
                var model = modelAwait.GetAwaiter().GetResult();

                //Find hte root
                var rootAwait = sourceDocument.GetSyntaxRootAsync().ConfigureAwait(false);
                while (!rootAwait.GetAwaiter().IsCompleted) yield return State.FindingRoot;
                var root = rootAwait.GetAwaiter().GetResult();

                //Find the symbol
                var syntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                var symbol = model.GetDeclaredSymbol(syntax);

                //Find the references
                var referencesAwait = SymbolFinder.FindReferencesAsync(symbol, sourceDocument.Project.Solution).ConfigureAwait(false);
                while (!referencesAwait.GetAwaiter().IsCompleted) yield return State.FindingSymbol;
                var references = referencesAwait.GetAwaiter().GetResult();
                results.AddRange(references.SelectMany(s => s.Locations).Select(loc => new AnalyticResult(loc)));
            }

            yield return State.Done;
        }

        /// <summary>
        /// Finds references using Roslyn by the async paradigm.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<AnalyticResult>> FindReferencesAsync(UnityEngine.Object asset)
        {
            //Get the parts
            if (asset is MonoBehaviour monoBehaviour)
            {
                asset = MonoScript.FromMonoBehaviour(monoBehaviour);
            } 
            else if (asset is ScriptableObject scriptableObject)
            {
                asset = MonoScript.FromScriptableObject(scriptableObject);
            }

            //Make sure its a monoscript
            if (!(asset is MonoScript))
                throw new ArgumentException("Supplied argument is not a MonoScript");

            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
            string scriptPath = Path.GetFullPath(UnityEditor.AssetDatabase.GetAssetPath(asset));

            //Prepare paths
            string folder = Application.dataPath + "/../";
            string projectPath = Directory.EnumerateFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly).First();
            projectPath = @"D:\Users\Lachee\Documents\Unity Projects\DistanceJam\Assembly-CSharp.csproj";

            //Prepare workspace
            var workspace = new AdhocWorkspace(); 

            //Prepare solution
            var solId = SolutionId.CreateNewId();
            var solutionInfo = SolutionInfo.Create(solId, VersionStamp.Default);
            var solution = workspace.AddSolution(solutionInfo);

            //Prepare the project
            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Assembly-CSharp", "Assembly-CSharp", "C#", projectPath);
            var project = workspace.AddProject(projectInfo);

            //Prepare the documents
            Document sourceDocument = null;
            await Task.Run(() => { 
                XmlDocument xml = new XmlDocument();
                xml.Load(projectPath);
                var elements = xml.GetElementsByTagName("Compile");
                foreach(XmlElement element in elements)
                {
                    var includePath = Path.GetFullPath(Path.GetDirectoryName(projectPath) + "/" +  element.GetAttribute("Include"));

                    //workspace.AddDocument(compileDocument);
                    var name = Path.GetFileName(includePath);
                    var fileContent = File.ReadAllText(includePath);
                    var src = SourceText.From(fileContent);
                    var doc = project.AddDocument(name, src, filePath: includePath);
                    project = doc.Project;

                    if (includePath.Equals(scriptPath))
                        sourceDocument = doc;
                }
            });

            workspace.TryApplyChanges(solution);

            if (sourceDocument == null) return null;

            var model = await sourceDocument.GetSemanticModelAsync();                                                           //Get the semantic model
            var root = await sourceDocument.GetSyntaxRootAsync();                                                               //Get the syntax
            var syntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();                                       //Find the first ClassDeclaration within the syntax
            var symbol = model.GetDeclaredSymbol(syntax);                                                                       //Get the symbol based of the class declaration

            var references = await SymbolFinder.FindReferencesAsync(symbol, sourceDocument.Project.Solution);                               //Find references
            return references.SelectMany(s => s.Locations).Select(loc => new AnalyticResult(loc));
        }


    }

}

/*
            foreach(var reference in references)
            {
                foreach(var location in reference.Locations)
                {
                    Debug.LogFormat("{0}:\t\t{1} ({2})", reference.Definition.Name, location.Document.FilePath, location.Location.GetLineSpan().StartLinePosition.Line);
                }
            }
*/