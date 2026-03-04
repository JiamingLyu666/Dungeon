using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyAutoSetup
{
    private static bool isSubscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SetupEnemiesInScene(SceneManager.GetActiveScene());

        if (isSubscribed) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        isSubscribed = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupEnemiesInScene(scene);
    }

    private static void SetupEnemiesInScene(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            Transform[] allChildren = rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                string lowerName = child.name.ToLowerInvariant();

                bool isTargetCube =
                    lowerName == "cube" ||
                    lowerName.StartsWith("cube (") ||
                    lowerName == "cude" ||
                    lowerName.StartsWith("cude (");

                if (!isTargetCube) continue;

                if (child.GetComponent<EnemyChasePlayer>() == null)
                {
                    child.gameObject.AddComponent<EnemyChasePlayer>();
                }
            }
        }
    }
}