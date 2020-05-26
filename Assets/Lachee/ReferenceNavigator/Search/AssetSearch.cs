

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Lachee.ReferenceNavigator.Search
{
    /// <summary>
    /// Searches the assets
    /// </summary>
    public static class AssetSearch
    {
        /// <summary>
        /// Last path to the asset we are searching
        /// </summary>
        public static string LastSearchedAsset { get; private set; }
        private static PropertyInfo inspectorModeInfo;
        static AssetSearch()
        {
            inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }


        private static string[] FileExtension(SearchType flags)
        {
            List<string> extensions = new List<string>(4);
            if (flags.HasFlag(SearchType.Prefab)) extensions.Add("prefab");
            if (flags.HasFlag(SearchType.Scene)) extensions.Add("unity");
            if (flags.HasFlag(SearchType.Script)) extensions.Add("cs");
            if (flags.HasFlag(SearchType.Asset)) extensions.Add("asset");
            if (flags.HasFlag(SearchType.Material)) extensions.Add("mat");
            return extensions.ToArray();
        }

        /// <summary>
        /// Counts occurances of every asset
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="incremental"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        public static IEnumerator CountAssetsEnumerator(SearchType filter, bool incremental, Dictionary<string, CountResult> results)
        {
            //Find the assets
            var assets = FindAssets(filter);
            foreach(var asset in assets) {

                //Skip asset types that are large and cannot have references.
                if (asset.assetType == typeof(LightingDataAsset)) continue;
                if (asset.assetType == typeof(AudioClip)) continue;
                if (asset.assetType == typeof(VideoClip)) continue;
                if (asset.assetType == typeof(Texture2D)) continue;

                //Prepare the reference and link it back
                LastSearchedAsset = asset.assetPath;
                yield return null;

                var enumerator = YamlSearch.CountReferences(asset.assetPath, results);
                while (enumerator.MoveNext())
                {
                    if (incremental)
                        yield return null;
                }
            }
        }

        /// <summary>
        /// Reads the contents of the assets and attempts to find references to the searching object. For efficicency, it will return null for invalid results.
        /// </summary>
        /// <param name="searchingObject"></param>
        /// <param name="types"></param>
        public static IEnumerator FindAssetsEnumerator(Object searchingObject, SearchType types, List<AssetResult> results, long fileSizeLimit = 10000000, bool withProgressBar = false)
        {
            if (searchingObject == null) 
                yield break;

            //Get the GUID and search type
            string guid;
            long instanceId;
            System.Type searchType = searchingObject is MonoScript monoScript ? monoScript.GetClass() : AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GetAssetPath(searchingObject));
            bool canSearchComponents = typeof(Component).IsAssignableFrom(searchType);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(searchingObject, out guid, out instanceId))
            {
                Debug.LogError("Failed to get GUID");
                yield break;
            }


            var unloadedResults = FindAssets(types);
            var totalProgress = unloadedResults.Count();
            List<AssetResult> loadedResults = new List<AssetResult>(totalProgress);

            var progress = 0;
            string fileContents;
            List<Component> loadedComponents    = new List<Component>(10);
            List<GameObject> rootGameObjects    = new List<GameObject>(10);

            var orderedUnloadedResults = unloadedResults.OrderBy(r => new FileInfo(r.assetPath).Length).Where(r => new FileInfo(r.assetPath).Length < fileSizeLimit);
            foreach (var foundResult in orderedUnloadedResults)
            {
                //Update the progress bar
                if (withProgressBar)
                    EditorUtility.DisplayProgressBar("Searching File", foundResult.assetPath, progress++ / (float)totalProgress);

                //We dont care about these
                if (foundResult.assetType == typeof(LightingDataAsset)) continue;
                if (foundResult.assetType == typeof(AudioClip)) continue;
                if (foundResult.assetType == typeof(VideoClip)) continue;
                if (foundResult.assetType == typeof(Texture2D)) continue;

                //Read the file contents
                LastSearchedAsset = foundResult.assetPath;
                fileContents = File.ReadAllText(foundResult.assetPath);
                if (!fileContents.Contains(guid))
                {
                    //Skip if the total read bytes is a great amount.
                    yield return null;
                }
                else
                {
                    //Dupe the result
                    var result = foundResult;

                    //Load the asset and find all sub components
                    result.assetObject = AssetDatabase.LoadAssetAtPath(foundResult.assetPath, foundResult.assetType);
                    if (result is ComponentResult componentResult)
                    {
                        var localFileReferences = YamlSearch.FindLocalFileReferences(fileContents, guid).ToLookup(k => k.fileId);
                        componentResult.FindReferences(fileContents, guid, localFileReferences);
                    }

                    //Add an item
                    results.Add(result);
                    yield return null;
                }
            }

            //Clear the progress bar
            if (withProgressBar) EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Finds all assets of the type.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static IEnumerable<AssetResult> FindAssets(SearchType filter)
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in assetPaths)
            {
                //Validate its folder
                if (!path.StartsWith("Assets/")) continue;

                //Validate its search
                string extension = Path.GetExtension(path);
                if (extension.Length <= 1) continue;
                extension = extension.Substring(1);

                //Get the type
                SearchType searchType;
                switch (extension)
                {
                    default:
                        searchType = SearchType.Files;
                        break;

                    case "mat":
                        searchType = SearchType.Material;
                        break;

                    case "prefab":
                        searchType = SearchType.Prefab;
                        break;

                    case "unity":
                        searchType = SearchType.Scene;
                        break;

                    case "cs":
                        searchType = SearchType.Script;
                        break;

                    case "asset":
                        searchType = SearchType.Asset;
                        break;
                }

                //validate the type
                if (!filter.HasFlag(SearchType.Files) && !filter.HasFlag(searchType))
                    continue;

                //Dont load textures, sound or lighting
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                //return scene
                switch (searchType)
                {
                    default:
                        yield return new AssetResult()
                        {
                            assetPath = path,
                            assetType = assetType,
                            searchableType = searchType
                        };
                        break;

                    //Return scene
                    case SearchType.Scene:
                        yield return new SceneResult()
                        {
                            assetPath = path,
                            assetType = assetType,
                            searchableType = searchType
                        };
                        break;

                    //Return prefab
                    case SearchType.Prefab:
                        yield return new PrefabResult()
                        {
                            assetPath = path,
                            assetType = assetType,
                            searchableType = searchType
                        };
                        break;

                    //Return Script
                    case SearchType.Script:
                        yield return new ScriptResult()
                        {
                            assetPath = path,
                            assetType = assetType,
                            searchableType = searchType
                        };
                        break;
                }

            }
        }

        /// <summary>
        /// Gets the local identifier ID of a component
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static int GetLocalIdentifier(this Component component)
        {
            if (component == null) return 0;
            SerializedObject serializedObject = new SerializedObject(component);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!
            return localIdProp.intValue;
        }
    }
}
