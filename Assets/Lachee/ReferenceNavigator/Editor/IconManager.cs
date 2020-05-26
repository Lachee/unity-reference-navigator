using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Lachee.ReferenceNavigator.Editor
{
    internal static class IconManager
    {
        public static string BasePath = "Assets/Lachee/ReferenceNavigator/Icons/";

        public static Texture Eye => GetCacheTexture("eye");
        public static Texture FileImport => GetCacheTexture("file-import");
        public static Texture FileSearch => GetCacheTexture("file-search");
        public static Texture LocationArrow => GetCacheTexture("location-arrow");
        public static Texture Location => GetCacheTexture("location");
        public static Texture Search => GetCacheTexture("search");

        public static readonly GUILayoutOption[] LayoutOptions = new GUILayoutOption[] { GUILayout.Width(30), GUILayout.Height(18) };

        private static Dictionary<string, Texture> _cache = new Dictionary<string, Texture>(6);

        /// <summary>
        /// Gets a texture and stores it in a cache if requried
        /// </summary>
        /// <param name="name"></param>
        /// <param name="recache"></param>
        /// <returns></returns>
        public static Texture GetCacheTexture(string name, bool recache = false)
        {
            //Check the cache
            if (!recache && _cache.TryGetValue(name, out var t))
                return t;

            //Load the file
            string filePath = BasePath + name + ".png";
            if (!File.Exists(filePath)) {
                Debug.LogError("The icon " + name + " does not exist!");
                return null;
            }

            //Load the texture and store
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(filePath);
            _cache[name] = texture;
            return texture;
        }
    }
}
