using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Lachee.ReferenceNavigator.Search;
using System.Collections;

namespace Lachee.ReferenceNavigator.Editor
{
    public class ReferenceCountWindow : EditorWindow
    {
        enum Order
        {
            Unsorted,
            Descending,
            Ascending
        }

        private Vector2 scroll = Vector2.zero;

        /// <summary>
        /// Is the searching currently paused
        /// </summary>
        public bool IsPaused => pauseSearching;
        [SerializeField] private bool pauseSearching = false;

        public int previousReferenceCount {
            get => EditorPrefs.GetInt("_lastReferenceCount", 0);
            set { EditorPrefs.SetInt("_lastReferenceCount", value); }
        }


        public bool IsSearching => enumerator != null;
        private IEnumerator enumerator;

        /// <summary>
        /// Searches one match at a time
        /// </summary>
        private bool incrementalSearch = false;

        /// <summary>
        /// The type we should be searching
        /// </summary>
        private SearchType searchType = SearchType.Scene | SearchType.Prefab | SearchType.Material | SearchType.Asset;

        private Order order = Order.Descending;

        /// <summary>
        /// The counts
        /// </summary>
        private Dictionary<string, CountResult> counts;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/References Count")]
        static ReferenceCountWindow OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            ReferenceCountWindow window = (ReferenceCountWindow)EditorWindow.GetWindow(typeof(ReferenceCountWindow));
            window.titleContent = new GUIContent("Reference Count");
            window.Show();
            return window;
        }


        /// <summary>
        /// Initial Awake
        /// </summary>
        private void OnEnable() {
            if (!IsPaused) Count();
        }

        /// <summary>
        /// Counts
        /// </summary>
        public void Count()
        {
            //Store the last cap
            if (counts != null && counts.Count != 0)
                previousReferenceCount = counts.Count;

            //Clear the counts and enumerate
            counts = new Dictionary<string, CountResult>(1000);
            enumerator = AssetSearch.CountAssetsEnumerator(searchType, incrementalSearch, counts);
            pauseSearching = false;
        }

        /// <summary>
        /// Pauses the count
        /// </summary>
        public void Pause() { pauseSearching = true;  }

        /// <summary>
        /// Unpauses the count
        /// </summary>
        public void Unpause() { pauseSearching = false; }

        void OnGUI()
        {
            //Order selection
            this.order = (Order) EditorGUILayout.EnumPopup(new GUIContent("Display Order", "How to order the counts"), this.order);

            EditorGUI.BeginDisabledGroup(IsSearching);
            incrementalSearch = EditorGUILayout.Toggle(new GUIContent("Incremental Search", "Enables the search to take breaks in the middle of files. This will make large files not freeze the editor"), incrementalSearch);
            EditorGUI.EndDisabledGroup();


            //Search Button
            if (IsSearching)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (IsPaused && GUILayout.Button(new GUIContent("Resume", "Continues the search from the point it was paused")))
                        Unpause();

                    if (!IsPaused && GUILayout.Button(new GUIContent("Pause", "Pauses the search")))
                        Pause();

                    if (GUILayout.Button(new GUIContent("Stop", "Stops the search")))
                        enumerator = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Begin Count", "Starts search all the assets and counts their references")))
                    Count();
            }

            if (counts != null)
                DrawResults();
        }

        private void DrawProgressBar(Rect position, string text)
        {
            int max = previousReferenceCount;
            EditorGUI.ProgressBar(position, counts.Count / (float)max, text);
            //max = previousReferenceCount + Mathf.FloorToInt((counts.Count / (float)previousReferenceCount) * 100);
        }

        private void DrawResults()
        {
            //Draw helpbox
            if (IsSearching)
            {
                //EditorGUILayout.HelpBox(, MessageType.None);

                var rect = EditorGUILayout.GetControlRect();
                DrawProgressBar(rect, "Searching " + AssetSearch.LastSearchedAsset);
            }
            else
            {
                EditorGUILayout.HelpBox("Searched " + counts.Count + " assets", MessageType.None);
            }


            //Draw scroll view
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.BeginVertical();
            {
                //Iterate over the sorted results
                var enumeratable = counts.Where(kp => kp.Value.valid);
                switch(order)
                {
                    default: break;
                    case Order.Ascending:
                        enumeratable = enumeratable.OrderBy(kp => kp.Value.count);
                        break;

                    case Order.Descending:
                        enumeratable = enumeratable.OrderByDescending(kp => kp.Value.count);
                        break;
                }

                foreach (var keypair in enumeratable)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Draw the objects
                        EditorGUILayout.ObjectField(keypair.Value.asset, typeof(Object), true);
                        EditorGUILayout.LabelField(keypair.Value.count.ToString());
                        if (GUILayout.Button(new GUIContent(IconManager.Search, "Find All References to this asset"), IconManager.LayoutOptions))
                        {
                            FindReferencesWindow.OpenWindowSearchObject(keypair.Value.asset);
                            Pause();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            if (enumerator != null && !pauseSearching)
            {
                if (!enumerator.MoveNext() || !enumerator.MoveNext() || !enumerator.MoveNext())
                {
                    //We have finished.
                    enumerator = null;
                    pauseSearching = true;
                    previousReferenceCount = counts.Count;
                } 
                else
                {
                    this.Repaint();
                }
            }
        }

        /// <summary>
        /// Sets the font and returns the previous one
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        Font SetFont(Font font)
        {
            var p = GUI.skin.font;
            GUI.skin.font = font;
            return p;
        }
    }
}