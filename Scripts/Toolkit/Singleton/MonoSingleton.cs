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

using UnityEngine;

namespace TwoSleepyCats.Patterns.Singleton
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static readonly object _lock = new object();
        private static T _instance;
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance of {typeof(T)} already destroyed on application quit. Won't create again.");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<T>();

                            if (_instance == null)
                            {
                                GameObject singleton = new GameObject();
                                _instance = singleton.AddComponent<T>();
                                singleton.name = typeof(T).Name + " (Singleton)";

                                DontDestroyOnLoad(singleton);
                                _instance.Initialize();
                            }
                            else
                            {
                                DontDestroyOnLoad(_instance.gameObject);
                                _instance.Initialize();
                            }
                        }
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Another instance of {typeof(T)} already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        protected virtual void Initialize()
        {
            // Override this method in derived classes for initialization logic
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public virtual void Dispose()
        {
            if (_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
                else
                {
                    DestroyImmediate(_instance.gameObject);
                }
                _instance = null;
            }
        }

        /// <summary>
        /// Check if singleton instance exists without creating one
        /// </summary>
        public static bool HasInstance => _instance != null && !_applicationIsQuitting;

        /// <summary>
        /// Manually set the singleton instance (use with caution)
        /// </summary>
        public static void SetInstance(T instance)
        {
            if (_instance != null && _instance != instance)
            {
                Debug.LogWarning($"[MonoSingleton] Overriding existing instance of {typeof(T)}");
                if (Application.isPlaying)
                {
                    Destroy(_instance.gameObject);
                }
            }
            
            _instance = instance;
            if (_instance != null)
            {
                DontDestroyOnLoad(_instance.gameObject);
            }
        }
    }
}