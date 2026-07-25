#region

using UnityEngine;

#endregion

namespace Managers
{
    /// <summary>
    /// A persistent Singleton base class which
    /// automatically handles instance checking in Awake()
    /// and marks the object as DontDestroyOnLoad.
    /// </summary>
    /// <typeparam name="T">The type of the class inheriting from this (e.g., ExistenceManager)</typeparam>
    public abstract class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour // defensive programming ensures the T is always a MonoBehaviour and not some random class
    {
        /// <summary>
        /// The global, static instance of this Singleton for other objects to reference instead of directly referencing the class.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Checks if the inheriting object is the same as the type passed into the Singleton
        /// which prevents weird bugs like - public class HUDManager : Singleton&lt;ExistenceManager&gt;.
        /// Then it deletes any duplicate versions of this object before assigning the variable to a new one.
        /// </summary>
        protected virtual void Awake()
        {
            if (this.GetType() != typeof(T))
            {
                Debug.LogError($"Singleton Inheritance Error: {this.GetType().FullName} " +
                               $"is trying to be the Singleton for {typeof(T).FullName}.\nDestroying duplicate object.");
                Destroy(gameObject);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Duplicate Singleton: {typeof(T)}. Destroying this new one.");
                // Destroys this object, as it would be a duplicate
                Destroy(gameObject);
                return;
            }


            // The only instance.
            Instance = this as T;

            // Doesn't destroy this object when a new scene loads as it should be continuous across scenes
            DontDestroyOnLoad(gameObject);
        }
    }
}