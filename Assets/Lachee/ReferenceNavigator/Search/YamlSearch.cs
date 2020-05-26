using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lachee.ReferenceNavigator.Search
{
    /// <summary>
    /// Searches Unity YAML files for specific results
    /// </summary>
    public static class YamlSearch
    {
        public static readonly Regex GUIDRegex = new Regex("[a-z0-9]{32}", RegexOptions.Compiled);

        /// <summary>
        /// Extracts a very rough and rudementry keypair of the YAML object.
        /// </summary>
        /// <param name="fileContents">The YAML source</param>
        /// <param name="localId">The local instance id</param>
        /// <returns></returns>
        public static Dictionary<string, string> ExtractInstanceDetails(string fileContents, string localId)
        {
            //Get the start position and end position
            int start = fileContents.IndexOf("&" + localId);
            if (start < 0) return null;
            int end = fileContents.IndexOf("--- !u!", start);
            if (end < 0) end = fileContents.Length;

            //Prepare a sub to work with
            string section = fileContents.Substring(start, end - start).Trim();
            string[] lines = section.Split('\n');

            //Prepare metadata
            Dictionary<string, string> metadata = new Dictionary<string, string>(lines.Length);
            string lastProperty = null;

            //Get the type
            metadata["type"] = lines[1].Trim(' ', '\r', '\n', '\t', ':');
            metadata["localId"] = localId;

            //Fill the metadata with each line
            for (int i = 0; i < lines.Length; i++)
            {
                //Get the indentation, skip if we are the first few items
                int indentation = CountIndentation(lines[i]) / 2;
                if (indentation == 0) continue;

                //If we have more than 1 indentation, then we are a sub-value. Lets just append them to the other file, (we dont care)
                if (indentation > 1 || lines[i].Trim().StartsWith("-"))
                {
                    if (lastProperty != null)
                    {
                        metadata[lastProperty] += "\n" + lines[i];
                    }

                    continue;
                }

                //Now we split
                int colonPosition = lines[i].IndexOf(':');
                string name = lines[i].Substring(0, colonPosition).Trim();
                string argument = lines[i].Substring(colonPosition).Trim();

                //Add it to the meta
                if (metadata.ContainsKey(name))
                {
                    Debug.LogWarning("Duplicated Key");
                }
                metadata[name] = argument;
                lastProperty = name;
            }

            //Return the metadata
            return metadata;
        }

        /// <summary>
        /// Counts all the references to different GUIDs in this asset
        /// </summary>
        /// <param name="path">The path of the asset to load</param>
        /// <param name="results">The resulting counts</param>
        /// <returns></returns>
        public static IEnumerator CountReferences(string path, Dictionary<string, CountResult> results)
        {
            //Read assets and perform a regex match
            string source = File.ReadAllText(path);
            var matches = GUIDRegex.Matches(source);

            //For every match, increment the count for it. If we need to, create the count
            foreach (Match match in matches)
            {
                //Prepare the GUID
                string guid = match.Value.Trim();

                //Make sure the item exists
                if (!results.ContainsKey(guid))
                    results.Add(guid, new CountResult(guid));

                //Increment the vount
                results[guid].Increment();

                //Find the instanceId
                /*
                int cstart = source.LastIndexOf("--- !u!", match.Index);
                int cend = source.IndexOf(':', cstart);
                string header = source.Substring(cstart, cend - cstart);
                if (header.Contains("MonoBehaviour"))
                {
                    string[] parts = header.Split(' ');
                    string instanceId = parts[3].Trim(' ', '&', '\n', '\r', '\t');
                    
                }
                */

                //Yield so we can catch up
                yield return guid;
            }
        }

        /// <summary>
        /// Looks through the source content for every object instance that contains the GUID. It will return a result for each instance id if finds with the GUID
        /// </summary>
        /// <param name="fileContents"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static IEnumerable<LocalFileReferenceResult> FindLocalFileReferences(string fileContents, string guid) 
        { 
            int cursor = 0;
            int commentCursor = 0;
            int commentCursorEnd = 0;
            int typeCursorPositionEnd = 0;

            string monobehaviourId = null;

            while ((cursor = fileContents.IndexOf(guid, cursor+1)) > 0)
            {
                //Find the comment by looking backwareds for --- !u!
                commentCursor = fileContents.LastIndexOf("--- !u!", cursor);
                if (commentCursor < 0) continue;

                commentCursorEnd = fileContents.IndexOf('\n', commentCursor);
                string comment = fileContents.Substring(commentCursor, commentCursorEnd - commentCursor).Trim('\r', '\n', '\t', ' ', '-', ':');
                string[] commentParts = comment.Split('&');

                //Find hte type
                typeCursorPositionEnd = fileContents.IndexOf('\n', commentCursorEnd + 1);
                string type = fileContents.Substring(commentCursorEnd, typeCursorPositionEnd - commentCursorEnd).Trim('\r', '\n', '\t', ' ', '-', ':');

                yield return new LocalFileReferenceResult()
                {
                    definitionPosition  = cursor,
                    commentPosition     = commentCursor,
                    typePosition        = commentCursorEnd + 1,
                    fileId              = commentParts[1],
                    type                = type
                };

                if (type == "MonoBehaviour")
                    monobehaviourId = commentParts[1];
            }

            //MonoBehaviour isn't null, so lets look for every reference to that
            if (monobehaviourId != null)
            {

                cursor = 0;
                while ((cursor = fileContents.IndexOf(monobehaviourId, cursor + 1)) > 0)
                {
                    //We dont care about self references
                    if (fileContents[cursor - 1] == '&') continue;

                    //Find the comment by looking backwareds for --- !u!
                    commentCursor = fileContents.LastIndexOf("--- !u!", cursor);
                    if (commentCursor < 0) continue;

                    commentCursorEnd = fileContents.IndexOf('\n', commentCursor);
                    string comment = fileContents.Substring(commentCursor, commentCursorEnd - commentCursor);//.Trim('\n', '\t', ' ', '-');
                    string[] commentParts = comment.Split('&');

                    //Find hte type
                    typeCursorPositionEnd = fileContents.IndexOf('\n', commentCursorEnd + 1);
                    string type = fileContents.Substring(commentCursorEnd, typeCursorPositionEnd - commentCursorEnd).Trim('\n', '\t', ' ', '-', ':');

                    yield return new LocalFileReferenceResult()
                    {
                        definitionPosition = cursor,
                        commentPosition = commentCursor,
                        typePosition = commentCursorEnd + 1,
                        fileId = commentParts[1],
                        type = type
                    };
                }

            }
        }

        /// <summary>
        /// Counts indentation
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static int CountIndentation(string content)
        {
            for (int i = 0; i < content.Length; i++)
                if (content[i] != ' ' && content[i] != '\t') return i;             
            return 0;
        }

        public struct LocalFileReferenceResult
        {
            public int definitionPosition;
            public int commentPosition;
            public int typePosition;
            public string fileId;
            public string type;

            public static implicit operator string(LocalFileReferenceResult result) { return result.fileId; }
        }
    }
}
