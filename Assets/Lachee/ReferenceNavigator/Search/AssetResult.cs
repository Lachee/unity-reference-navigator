
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace Lachee.ReferenceNavigator.Search
{
    /// <summary>
    /// Asset search results
    /// </summary>
    public class AssetResult
    {
        /// <summary>
        /// Name of the object
        /// </summary>
        public string name { get { return assetObject ? assetObject.name : null; } }

        /// <summary>
        /// The GUID of the asset
        /// </summary>
        public string guid => _guid == null ? (_guid = AssetDatabase.AssetPathToGUID(assetPath)) : _guid;
        private string _guid = null;

        /// <summary>
        /// Path of the object
        /// </summary>
        public string assetPath;

        /// <summary>
        /// The object itself
        /// </summary>
        public Object assetObject;

        /// <summary>
        /// The type of the asset
        /// </summary>
        public System.Type assetType;

        /// <summary>
        /// What we searched to get the object
        /// </summary>
        public SearchType searchableType;

    }

    /// <summary>
    /// Results for components
    /// </summary>
    public abstract class ComponentResult : AssetResult
    {
        /// <summary>
        /// Subreferences the object has
        /// </summary>
        public List<Object> references = new List<Object>(0);

        public abstract void FindReferences(string fileContents, string guid, ILookup<string, YamlSearch.LocalFileReferenceResult> localFileIds);
    }

    /// <summary>
    /// Scene Results
    /// </summary>
    public class SceneResult : ComponentResult
    {
        /// <summary>
        /// Checks if the scene is valid
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return SceneManager.GetSceneByPath(assetPath).IsValid();
        }

        public override void FindReferences(string fileContents, string guid, ILookup<string, YamlSearch.LocalFileReferenceResult> localFileIds)
        {
            var scene = SceneManager.GetSceneByPath(assetPath);
            if (scene.IsValid())
            {
                var gameObjects = scene.GetRootGameObjects();
                foreach (var gameObject in gameObjects)
                {
                    var components = gameObject.GetComponentsInChildren<Component>(true);
                    references.AddRange(components.Where(c => c != null && (localFileIds.Contains(c.GetLocalIdentifier().ToString()) ||  assetObject.GetType().IsAssignableFrom(c.GetType()))));
                }
            } 
            else
            {
                foreach(var kp in localFileIds)
                {
                    var doc = YamlSearch.ExtractInstanceDetails(fileContents, kp.Key);
                    if (doc.TryGetValue("m_Script", out var scriptRaw))
                    {
                        var match = YamlSearch.GUIDRegex.Match(scriptRaw);
                        if (match.Success)
                        {
                            var objAssetPath = AssetDatabase.GUIDToAssetPath(match.Value);
                            var obj = AssetDatabase.LoadAssetAtPath(objAssetPath, typeof(Object));
                            references.Add(obj);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Prefab Results
    /// </summary>
    public class PrefabResult : ComponentResult
    {
        public override void FindReferences(string fileContents, string guid, ILookup<string, YamlSearch.LocalFileReferenceResult> localFileIds)
        {
            var components = ((GameObject)(assetObject)).GetComponentsInChildren<Component>(true);
            references = new List<Object>(components.Where(c => localFileIds.Contains(c.GetLocalIdentifier().ToString()) || assetType.IsAssignableFrom(c.GetType())));
        }
    }

    /// <summary>
    /// Script Results
    /// </summary>
    public class ScriptResult : AssetResult
    {
        public void FindScriptReferences()
        {
        }
    }
}
