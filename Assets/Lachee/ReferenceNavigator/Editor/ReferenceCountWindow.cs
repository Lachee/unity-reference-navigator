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

        public bool IsSearching => enumerator != null;
        private IEnumerator enumerator;

        /// <summary>
        /// Searches one match at a time
        /// </summary>
        private bool incrementalSearch = true;

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
            //Clear the counts and enumerate
            counts = new Dictionary<string, CountResult>(150);
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
            this.order = (Order) EditorGUILayout.EnumPopup("Display Order", this.order);

            //Search Button
            if (IsSearching)
            {
                if (IsPaused && GUILayout.Button("Continue"))
                    Unpause();

                if (!IsPaused && GUILayout.Button("Pause"))
                    Pause();

                if (GUILayout.Button("Stop"))
                    enumerator = null;
            }
            else
            {
                incrementalSearch = EditorGUILayout.Toggle("Incremental search", incrementalSearch);
                if (GUILayout.Button("Begin"))
                    Count();
            }

            if (counts != null)
                DrawResults();
        }

        private void DrawResults()
        {
            //Draw helpbox
            if (IsSearching)
            {
                EditorGUILayout.HelpBox("Searching " + AssetSearch.LastSearchedAsset, MessageType.None);
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
                        if (GUILayout.Button(IconManager.Search, IconManager.LayoutOptions))
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
                    enumerator = null;
                    pauseSearching = true;
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