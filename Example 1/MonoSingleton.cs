using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Melai.LevelManager
{
    /// <summary>
    /// Class for implementing the singleton pattern
    /// </summary>
    /// <typeparam name="T">Type for singleton</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.Log(typeof(T).ToString() + " is NULL");
                }

                return _instance;
            }
        }

        private void Awake()
        {
            // check for other instances
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                // get instance
                _instance = this as T;

                Init();
            }
        }

        /// <summary>
        /// You can add to this method what should be done during initialization
        /// </summary>
        public virtual void Init()
        {
            // optional to override
        }
    }
}
