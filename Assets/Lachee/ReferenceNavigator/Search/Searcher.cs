using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Lachee.ReferenceNavigator.Search
{
    /// <summary>
    /// Handles searching
    /// </summary>
    public class Searcher
    {
        public Object Subject { get; }
        public SearchType SearchType { get; }

        /// <summary>
        /// Asset path
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// Name of the searcher
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Results of the assets
        /// </summary>
        public IReadOnlyList<AssetResult> Assets => _assetResults;
        private List<AssetResult> _assetResults = new List<AssetResult>(10);
        private IEnumerator _assetEnumerator;

        /// <summary>
        /// Number of assets
        /// </summary>
        public int AssetCount => _assetResults == null ? 0 : _assetResults.Count;

        /// <summary>
        /// Results of analytics
        /// </summary>
        public IReadOnlyList<AnalyticResult> References => _analyticResults;
        private List<AnalyticResult> _analyticResults = new List<AnalyticResult>(1);
        private IEnumerator _analyticEnumerator;

        /// <summary>
        /// Number of analytic references we have found so far
        /// </summary>
        public int ReferenceCount => _analyticResults == null ? 0 : _analyticResults.Count;

        /// <summary>
        /// Is this currently searching
        /// </summary>
        public bool IsSearching => IsSearchingAssets || IsSearchingReferences;

        /// <summary>
        /// Is this currently searching the assets
        /// </summary>
        public bool IsSearchingAssets => _assetEnumerator != null;

        /// <summary>
        /// Is this currently searching references
        /// </summary>
        public bool IsSearchingReferences => _analyticEnumerator != null;

        public Searcher(Object subject, SearchType type)
        {
            this.Subject = subject;
            this.SearchType = type;
            this.Name = subject?.name;

            //If we are a MonoBehaviour or a ScriptableObject, we want to get hte base script
            if (Subject is MonoBehaviour monoBehaviour)
            {
                Subject = MonoScript.FromMonoBehaviour(monoBehaviour);
            }
            else if (Subject is ScriptableObject scriptableObject)
            {
                Subject = MonoScript.FromScriptableObject(scriptableObject);
            }

            //If the subject is a MonoScript and we have the search script
            if (type.HasFlag(SearchType.Script) && Subject is MonoScript)
                _analyticEnumerator = AnalyticSearch.FindReferencesEnumerator(Subject, _analyticResults);

            //If we are searching assets 
            if (type != SearchType.None)
                _assetEnumerator = AssetSearch.FindAssetsEnumerator(Subject, type, _assetResults);

            //Update our asset path
            this.AssetPath = AssetDatabase.GetAssetPath(Subject);
        }

        /// <summary>
        /// Step the results. Returns true when still going
        /// </summary>
        public bool Step()
        {
            //If we have the assets, then search it
            if (_assetEnumerator != null) {
                if (!_assetEnumerator.MoveNext()) {
                    _assetEnumerator = null;
                }
            }
            
            //If we have an analytics, then search it.
            if (_analyticEnumerator != null) {
                if (!_analyticEnumerator.MoveNext()) {
                    _analyticEnumerator = null;
                }
            }

            //Should we finish searching?
            return _assetEnumerator != null || _analyticEnumerator != null;
        }

        /// <summary>
        /// Do all the steps at once
        /// </summary>
        public void Complete()
        {
            while (Step()) ;
        }

    }
}
