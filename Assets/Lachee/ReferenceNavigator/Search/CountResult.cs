using UnityEditor;
using UnityEngine;

namespace Lachee.ReferenceNavigator.Search
{

    /// <summary>
    /// Stores result of counts, will include a reference to the asset and the path.
    /// </summary>
    public class CountResult
    {
        /// <summary>
        /// GUID of the asset
        /// </summary>
        public string guid { get; }
        /// <summary>
        /// Number of occurances
        /// </summary>
        public uint count { get; private set; }
        /// <summary>
        /// The asset
        /// </summary>
        public Object asset { get; }
        /// <summary>
        /// The path of the asset
        /// </summary>
        public string path { get; }

        /// <summary>
        /// Is the result entry valid (it has an object asset)
        /// </summary>
        public bool valid => asset != null;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="guid"></param>
        internal CountResult(string guid)
        {
            this.guid = guid;
            this.path = AssetDatabase.GUIDToAssetPath(guid);
            this.asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            this.count = 0;
        }

        /// <summary>
        /// Increments the tally by 1
        /// </summary>
        /// <returns></returns>
        internal uint Increment()
        {
            return ++count;
        }
    }

}
