using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Lachee.ReferenceNavigator.Search;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Lachee.ReferenceNavigator.Editor
{
    public class FindReferencesWindow : EditorWindow
    {
        const int MAX_SEARCH = 20;

        public static long frame = 0;

        private Vector2 scroll;

        /// <summary>
        /// The object we are searching for
        /// </summary>
        private Object searchObject;

        /// <summary>
        /// The type we should be searching
        /// </summary>
        [SerializeField] [Lachee.ReferenceNavigator.Attributes.EnumFlag] 
        private SearchType searchType = SearchType.Scene | SearchType.Prefab | SearchType.Script;

        /// <summary>
        /// All searchers
        /// </summary>
        private List<Searcher> searches = new List<Searcher>(MAX_SEARCH+1);

        /// <summary>
        /// Current searcher
        /// </summary>
        private Searcher current = null;
        private int currentIndex = 0;

        /// <summary>
        /// Flag indicating if we should search
        /// </summary>
        private bool doSearch = false;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/References Navigator")]
        public static FindReferencesWindow OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            FindReferencesWindow window = (FindReferencesWindow)EditorWindow.GetWindow(typeof(FindReferencesWindow));
            window.titleContent = new GUIContent("Reference Navigator");
            window.Show();
            return window;
        }

        [MenuItem("Assets/Find References In Project", priority = 21)]
        public static FindReferencesWindow OpenWindowSearchActive()
        {
            return OpenWindowSearchObject(Selection.activeObject);
        }

        /// <summary>
        /// Opens the window
        /// </summary>
        /// <param name="refence"></param>
        /// <returns></returns>
        public static FindReferencesWindow OpenWindowSearchObject(Object refence)
        {
            var window = OpenWindow();
            window.AddSearch(refence, SearchType.Scene | SearchType.Prefab | SearchType.Script, true);
            return window;
        }



        private void OnEnable()
        {
            this.searches = new List<Searcher>(MAX_SEARCH);
            doSearch = false;
            current = null;
            currentIndex = 0;
        }


        /// <summary>
        /// Searches an object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="searchType"></param>
        public void AddSearch(Object obj, SearchType searchType, bool clear = false)
        {
            //Clear the stack
            if (clear) {
                searches.Clear();
            }

            //Push a new item to the search stack.
            var search = new Searcher(obj, searchType);
            searches.Add(search);
            doSearch = true;

            //Update the current
            SetCurrent(search);

            //If we have to man, pop
            if (searches.Count >= MAX_SEARCH)
                searches.RemoveAt(0);
        }

        private void SetCurrent(Searcher searcher) {
            searchObject = searcher.Subject;
            searchType = searcher.SearchType;
            current = searcher;
            currentIndex = 0;
            foreach(var s in searches)
            {
                if (s == current) break;
                currentIndex++;
            }
        }

        /// <summary>
        /// Pauses the search
        /// </summary>
        public void Pause()
        {
            doSearch = false;
        }

        /// <summary>
        /// Continues the search
        /// </summary>
        public void Continue()
        {
            doSearch = true;
        }

        void OnGUI()
        {
            //Auto Search for debugging
            if (searches.Count == 0) AddSearch(searchObject, searchType, true);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(1));
                {
                    //Draw the header
                    DrawNavigation();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                {
                    //Draw the search
                    DrawSearchOptions();

                    //Draw the results
                    GUILayout.Label("Result", EditorStyles.boldLabel);
                    if (searches.Count > 0) DrawSearcher(current);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

        }


        void DrawNavigation()
        {
            /*
            int newIndex = GUILayout.Toolbar(currentIndex, searches.Select(q => new GUIContent(q.Name, AssetDatabase.GetCachedIcon(q.AssetPath))).ToArray(), GUILayout.Height(15));
            if (newIndex != currentIndex)
            {
                SetCurrent(searches.Skip(newIndex).First());
            }
            */
            for (int i = searches.Count-1; i >= 0; i--)
            {
                int maxdiff = 15;

                //Calculate the difference
                float diff = Mathf.Abs((float)currentIndex - i);
                float alpha = Mathf.Max(1f - (diff / maxdiff), 0.1f);

                var s = searches[i];
                GUI.color = new Color(1, 1, 1, alpha);
                var texture = AssetDatabase.GetCachedIcon(s.AssetPath);
                if (GUILayout.Button(new GUIContent(texture, s.Name), GUILayout.Height(25), GUILayout.Width(25)))
                {
                    SetCurrent(s);
                }
            }

            GUI.color = Color.white;
        }
        
        void DrawSearchOptions()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            searchObject = EditorGUILayout.ObjectField("Asset", searchObject, typeof(Object), false);
            searchType = (SearchType)EditorGUILayout.EnumFlagsField("Pattern", searchType);

            EditorGUILayout.BeginHorizontal();

            var newSearchType = SearchType.None;
            var overrideWithAll = false;
            if (GUILayout.Toggle((searchType & SearchType.Scene) != 0, "Scene", EditorStyles.miniButtonLeft))           newSearchType |= SearchType.Scene;
            if (GUILayout.Toggle((searchType & SearchType.Prefab) != 0, "Prefab", EditorStyles.miniButtonMid))          newSearchType |= SearchType.Prefab;
            if (GUILayout.Toggle((searchType & SearchType.Asset) != 0, "Assets", EditorStyles.miniButtonMid))           newSearchType |= SearchType.Asset;
            if (GUILayout.Toggle((searchType & SearchType.Script) != 0, "Scripts", EditorStyles.miniButtonMid))         newSearchType |= SearchType.Script;
            if (GUILayout.Toggle((searchType & SearchType.Material) != 0, "Materials", EditorStyles.miniButtonRight))   newSearchType |= SearchType.Material;
            if (GUILayout.Toggle((searchType & SearchType.Files) != 0, "Every File", EditorStyles.miniButton))          newSearchType |= SearchType.Files;

            searchType = overrideWithAll ? SearchType.Files : newSearchType;
            EditorGUILayout.EndHorizontal();


            //GUI.skin.font = fontawesome;

            //Find the references
            EditorGUILayout.BeginHorizontal();
            {
                //Search the object
                if (searchObject != null)
                {
                    if (searches.Count > 0 && current.IsSearching)
                    {
                        if (doSearch)
                        {
                            //if (GUILayout.Button("Step Searching"))
                            //    current.Step();
                        
                            //Pause the search
                            if (GUILayout.Button("Pause Searching"))
                                Pause();
                        } else
                        {
                            //Continue the search
                            if (GUILayout.Button("Continue Searching"))
                                Continue();
                        }


                        //Continue the search
                        if (GUILayout.Button("Restart Search"))
                            AddSearch(searchObject, searchType, true);

                    } else
                    {
                        //Start a new search, and clear
                        if (GUILayout.Button("Find In Project"))
                            AddSearch(searchObject, searchType, true);
                    }


                    //Show in the scene
                    if (GUILayout.Button("Show In Scene"))
                    {
                        ShowInScene();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawSearcher(Searcher searcher)
        {
            //Show the status boxes. they are on top because otherwise they would be in the scroll area
            GUILayout.Label(searcher.IsSearchingAssets ? "Searching " + AssetSearch.LastSearchedAsset : "Showing " + searcher.AssetCount + " Assets", EditorStyles.helpBox);
            if (searcher.SearchType.HasFlag(SearchType.Script))
                GUILayout.Label(searcher.IsSearchingReferences ? "Searching References..." : "Showing " + searcher.ReferenceCount + " References", EditorStyles.helpBox);

            //Show the scroll
            scroll = EditorGUILayout.BeginScrollView(scroll);
            {
                //Draw the assets
                DrawResults(searcher.Assets);

                //Draw the references
                if (searcher.SearchType.HasFlag(SearchType.Script))
                    DrawReferences(searcher.References);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawResults(IReadOnlyList<AssetResult> results)
        {
            if (results != null && results.Count != 0)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    var obj = result.assetObject;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.ObjectField(obj, obj.GetType(), false);

                        if (GUILayout.Button(new GUIContent(IconManager.Search, "Search for this asset"), IconManager.LayoutOptions))
                            AddSearch(obj, searchType, false);

                        //If its a scene, open the asset
                        if (result.assetObject is SceneAsset sceneAsset && GUILayout.Button(new GUIContent(IconManager.Eye, "Open scene and show instances"), IconManager.LayoutOptions))
                        {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                EditorSceneManager.OpenScene(result.assetPath, OpenSceneMode.Single);
                                ShowInScene();
                            }
                        }

                    }
                    EditorGUILayout.EndHorizontal();

                    //Draw subs
                    if (result is ComponentResult componentResult && componentResult.references.Any())
                    {
                        EditorGUI.indentLevel += 3;
                        EditorGUILayout.BeginVertical();
                        {
                            foreach (var sub in componentResult.references)
                                DrawSubResult(sub);
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUI.indentLevel -= 3;
                    }
                }
            }
        }

        void DrawSubResult(Object component)
        {
            if (component == null) return;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.ObjectField(component, component.GetType(), false);

                if (GUILayout.Button(new GUIContent(IconManager.Search, "Search for this asset"), IconManager.LayoutOptions))
                    AddSearch(component, searchType, false);

                if (GUILayout.Button(new GUIContent(IconManager.Location, "Ping component"), IconManager.LayoutOptions))
                {
                    //AssetDatabase.OpenAsset(component);
                    Selection.activeObject = component;
                    EditorGUIUtility.PingObject(component);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawReferences(IReadOnlyList<AnalyticResult> references)
        {
            if (references != null && references.Count != 0)
            {
                for (int i = 0; i < references.Count; i++)
                {
                    var reference = references[i];
                    var obj = reference.Object;
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (obj == null)
                        {
                            EditorGUILayout.TextField(reference.AssetPath);

                        } else
                        {
                            EditorGUILayout.ObjectField(obj, obj.GetType(), false);

                            if (GUILayout.Button(new GUIContent(IconManager.Search, "Search for this asset"), IconManager.LayoutOptions))
                                AddSearch(obj, searchType, false);

                            if (GUILayout.Button(new GUIContent(IconManager.FileImport, "Open Line " + reference.LineNumber), IconManager.LayoutOptions))
                                reference.Open();
                        }

                        //EditorGUILayout.ObjectField(obj.name, obj, typeof(MonoScript), false);
                        // if (GUILayout.Button("Find")) Search(obj, searchType);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void Update()
        {
            if (searches.Count == 0) return;

            //Search teh current item
            if (doSearch)
            {
                this.Repaint();
                if (!current.Step()) Pause();
            }
        }

        /// <summary>
        /// Shows the current object in the scene
        /// </summary>
        void ShowInScene()
        {
            string path = AssetDatabase.GetAssetPath(searchObject);
            Hierarchy.Search($"ref:{path}");
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