using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;

namespace Lachee.ReferenceNavigator.Search
{

    /// <summary>
    /// Results for the analytics search
    /// </summary>
    public class AnalyticResult
    {
        public string FilePath { get; }
        public string AssetPath { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
        public int Offset { get; }
        public UnityEngine.Object Object { get; }

        internal AnalyticResult(ReferenceLocation location)
        {
            var linespan = location.Location.GetLineSpan();
            FilePath = linespan.Path;
            AssetPath = "Assets" + Path.DirectorySeparatorChar + GetRelativePath(FilePath, UnityEngine.Application.dataPath);

            LineNumber = linespan.StartLinePosition.Line + 1;
            ColumnNumber = linespan.StartLinePosition.Character + 1;
            Offset = location.Location.SourceSpan.Start;
            Object = AssetDatabase.LoadAssetAtPath(AssetPath, typeof(MonoScript));
        }

        /// <summary>
        /// Opens the reference
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            if (Object != null)
                return AssetDatabase.OpenAsset(Object, LineNumber, ColumnNumber);

            //Process.Start("devenv.exe", $"/edit \"{FilePath}\" /command \"edit.goto {LineNumber}\"");
            Process.Start(FilePath);
            return true;
        }

        /// <summary>
        /// Gets a relative path
        /// </summary>
        /// <param name="filespec"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
