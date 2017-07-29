using System;
using System.Collections.Generic;

namespace pvgisjsonapi {
    public class Cacher {
        /// <summary>
        /// All cache entries.
        /// </summary>
        public static Dictionary<string, object> Items { get; set; }

        /// <summary>
        /// Get an item from cache storage.
        /// </summary>
        /// <typeparam name="T">Type to cast the data as.</typeparam>
        /// <param name="name">Name of stored item.</param>
        /// <param name="callback">Callback function to get data from if not stored.</param>
        /// <returns>Casted data.</returns>
        public static T Get<T>(string name, Func<T> callback = null) {
            if (Items == null) {
                Items = new Dictionary<string, object>();
            }

            if (!Items.ContainsKey(name) && callback != null) {
                try {
                    Items.Add(name, callback.Invoke());
                }
                catch (Exception ex) {
                    return default(T);
                }
            }

            var data = Items[name];

            if (data == null) {
                return default(T);
            }

            try {
                return (T) data;
            }
            catch {
                return default(T);
            }
        }

        /// <summary>
        /// Add/update data in storage.
        /// </summary>
        /// <param name="name">Name to store under.</param>
        /// <param name="data">Data to store.</param>
        public static void Set(string name, object data) {
            if (Items == null) {
                Items = new Dictionary<string, object>();
            }

            if (Items.ContainsKey(name)) {
                Items[name] = data;
            }
            else {
                Items.Add(name, data);
            }
        }
    }
}