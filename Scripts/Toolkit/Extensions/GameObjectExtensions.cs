using System.Runtime.CompilerServices;
using UnityEngine;

namespace OctoberStudio.Extensions
{
    public static class GameObjectExtensions
    {
        #region Simple Creation Methods
        
        /// <summary>
        /// Creates a new GameObject as child of this transform
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject CreateChild(this Transform parent)
        {
            var go = new GameObject();
            go.transform.SetParent(parent);
            go.transform.ResetLocal();
            return go;
        }

        /// <summary>
        /// Creates a new GameObject as child of this GameObject
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject CreateChild(this GameObject parent)
        {
            return parent.transform.CreateChild();
        }

        /// <summary>
        /// Creates a new GameObject with component as child
        /// </summary>
        public static T CreateChild<T>(this Transform parent) where T : Component
        {
            var go = parent.CreateChild();
            return go.AddComponent<T>();
        }

        /// <summary>
        /// Creates a new GameObject with component as child
        /// </summary>
        public static T CreateChild<T>(this GameObject parent) where T : Component
        {
            return parent.transform.CreateChild<T>();
        }

        /// <summary>
        /// Spawns GameObject from prefab at parent
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject Spawn(this GameObject prefab, Transform parent)
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.transform.ResetLocal();
            return instance;
        }

        /// <summary>
        /// Spawns GameObject from prefab at parent with component
        /// </summary>
        public static T Spawn<T>(this GameObject prefab, Transform parent) where T : Component
        {
            var instance = prefab.Spawn(parent);
            return instance.GetComponent<T>();
        }

        /// <summary>
        /// Spawns GameObject at world position
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject SpawnAt(this GameObject prefab, Vector3 position, Transform parent = null)
        {
            var instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            return instance;
        }

        /// <summary>
        /// Spawns GameObject at world position and rotation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject SpawnAt(this GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var instance = Object.Instantiate(prefab, position, rotation, parent);
            return instance;
        }

        #endregion

        #region Destruction Methods

        /// <summary>
        /// Safely destroys GameObject with null check
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SafeDestroy(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Destroys GameObject after delay
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyAfter(this GameObject gameObject, float delay)
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject, delay);
            }
        }

        /// <summary>
        /// Destroys all children
        /// </summary>
        public static void DestroyAllChildren(this GameObject gameObject)
        {
            if (gameObject == null) return;
            
            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(gameObject.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Destroys all children of transform
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            if (transform == null) return;
            
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        #endregion

        #region Component Management

        /// <summary>
        /// Gets component or adds it if missing
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Checks if GameObject has component
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        /// <summary>
        /// Removes component if it exists
        /// </summary>
        public static void RemoveComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null)
            {
                Object.Destroy(component);
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Sets active state and returns GameObject for chaining
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject SetActive(this GameObject gameObject, bool active)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(active);
            }
            return gameObject;
        }

        /// <summary>
        /// Toggles active state
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToggleActive(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }
        }

        /// <summary>
        /// Sets layer recursively
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;
            
            gameObject.layer = layer;
            
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if GameObject is valid (not null)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this GameObject gameObject)
        {
            return gameObject != null;
        }

        /// <summary>
        /// Sets parent and returns GameObject for chaining
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject SetParent(this GameObject gameObject, Transform parent)
        {
            if (gameObject != null)
            {
                gameObject.transform.SetParent(parent);
            }
            return gameObject;
        }

        /// <summary>
        /// Resets local transform and returns GameObject for chaining
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject ResetLocal(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                gameObject.transform.ResetLocal();
            }
            return gameObject;
        }

        #endregion

        #region Equipment Spawning Helpers

        /// <summary>
        /// Spawns multiple instances of prefab
        /// </summary>
        public static GameObject[] SpawnMultiple(this GameObject prefab, Transform parent, int count)
        {
            var instances = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                instances[i] = prefab.Spawn(parent);
            }
            return instances;
        }

        /// <summary>
        /// Spawns prefab and immediately gets component
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SpawnAndGet<T>(this GameObject prefab, Transform parent) where T : Component
        {
            return prefab.Spawn(parent).GetComponent<T>();
        }

        /// <summary>
        /// Spawns prefab with immediate setup action
        /// </summary>
        public static GameObject SpawnWithSetup(this GameObject prefab, Transform parent, System.Action<GameObject> setup)
        {
            var instance = prefab.Spawn(parent);
            setup?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// Spawns prefab with component setup
        /// </summary>
        public static T SpawnWithSetup<T>(this GameObject prefab, Transform parent, System.Action<T> setup) where T : Component
        {
            var component = prefab.SpawnAndGet<T>(parent);
            setup?.Invoke(component);
            return component;
        }

        #endregion
    }
}