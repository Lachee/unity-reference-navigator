using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lachee.ReferenceNavigator.Editor
{


    /// <summary>
    /// Utility for the Hierarchy.
    /// </summary>
    public static class Hierarchy
    {
        private static MethodInfo searchableEditorWindowSetSearchType;
        static Hierarchy()
        {
            searchableEditorWindowSetSearchType = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Hierarchy search mode
        /// </summary>
        public enum SearchMode
        {
            All = 0,
            Name = 1,
            Type = 2,
            Label = 3,
            AssetBundleName = 4
        }

        /// <summary>
        /// Sets the filter in the Hierarchy
        /// </summary>
        /// <param name="filter">The string filter</param>
        /// <param name="searchMode">The search mode</param>
        public static void Search(string filter, SearchMode searchMode = SearchMode.All)
        {
            SearchableEditorWindow[] windows = (SearchableEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));
            SearchableEditorWindow hierarchy = windows.First(w => w.GetType().ToString() == "UnityEditor.SceneHierarchyWindow");
            if (hierarchy == null) throw new System.InvalidOperationException("Cannot filter hirerarchy because it does not exist");
            object[] parameters = new object[] { filter, (SearchableEditorWindow.SearchMode)searchMode, false, false };
            searchableEditorWindowSetSearchType.Invoke(hierarchy, parameters);
        }
    }
}
