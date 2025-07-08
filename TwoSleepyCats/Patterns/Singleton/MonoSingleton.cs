// =============================================================================
// Two Sleepy Cats Studio - MonoBehaviour Singleton
// 
// Author: Two Sleepy Cats Development Team
// Version: 1.0.0 - Core Foundation
// Created: 2025
// 
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;
using UnityEngine;

namespace TwoSleepyCats.Patterns.Singleton
{
    /// <summary>
    /// MonoBehaviour Singleton pattern for Unity objects that need lifecycle methods
    /// </summary>
    /// <typeparam name="T">Type of the singleton class</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        [Obsolete("Obsolete")]
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";
                            if (_instance.PersistAcrossScenes)
                            {
                                DontDestroyOnLoad(singletonObject);
                            }
                            Debug.Log($"[MonoSingleton] An instance of {typeof(T)} was created: {singletonObject.name}");
                        }
                        else
                        {
                            if (_instance.PersistAcrossScenes)
                            {
                                DontDestroyOnLoad(_instance.gameObject);
                            }
                        }
                        _instance.Initialize();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Override this property to control whether the singleton should persist across scene loads
        /// </summary>
        protected virtual bool PersistAcrossScenes => true;

        /// <summary>
        /// Override this method for custom initialization logic
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Unity's Awake method - ensures singleton behavior
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                
                if (PersistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                Initialize();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Another instance of {typeof(T)} already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity's OnDestroy method - cleans up the singleton reference
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Called when the application is quitting
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// Manually destroy the singleton instance
        /// </summary>
        public static void DestroySingleton()
        {
            if (_instance != null)
            {
                if (_instance.gameObject != null)
                {
                    Destroy(_instance.gameObject);
                }
                _instance = null;
            }
        }

        /// <summary>
        /// Check if the singleton instance exists
        /// </summary>
        public static bool HasInstance => _instance != null;
    }
}