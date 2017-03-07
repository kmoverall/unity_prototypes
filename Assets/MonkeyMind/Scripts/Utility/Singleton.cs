using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
    protected static T instance;

    /**
       Returns the instance of this singleton.
    */
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting) {
                return null;
            }
            if (instance == null) {
                instance = (T)FindObjectOfType(typeof(T));
                if (instance == null) {
                    GameObject obj = new GameObject("[SINGLETON]:"+typeof(T));
                    instance = obj.AddComponent(typeof(T)) as T;
                    Debug.Log("An instance of " + typeof(T) +
                       " is needed in the scene. Adding default.");

                    DontDestroyOnLoad(instance);
                }
            }

            return instance;
        }
    }

    private static bool applicationIsQuitting = false;
    /// When Unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.
    /// If any script calls Instance after it have been destroyed, 
    ///   it will create a buggy ghost object that will stay on the Editor scene
    ///   even after stopping playing the Application. Really bad!
    /// So, this was made to be sure we're not creating that buggy ghost object.
    protected virtual void OnDestroy() {
        applicationIsQuitting = true;
    }

    protected virtual void OnApplicationQuit() {
        instance = null;
    }
}