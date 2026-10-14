using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerPositionSaver : MonoBehaviour
{
    private static PlayerPositionSaver instance;

    private static Dictionary<string, Vector3> savedPositions = new Dictionary<string, Vector3>();
    private static Dictionary<string, Quaternion> savedRotations = new Dictionary<string, Quaternion>();

    private string currentScene;
    private PlayerMovement3D movementScript;

    void Awake()
    {
        // Singleton enforcement
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Cache reference to movement script
        movementScript = GetComponent<PlayerMovement3D>();

        // Always force-enable movement on the surviving instance
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log("[PlayerPositionSaver] Ensured PlayerMovement3D is enabled.");
        }

        currentScene = SceneManager.GetActiveScene().name;

        // Restore saved position/rotation if available
        if (savedPositions.ContainsKey(currentScene))
            transform.position = savedPositions[currentScene];

        if (savedRotations.ContainsKey(currentScene))
            transform.rotation = savedRotations[currentScene];

        SceneManager.activeSceneChanged += OnSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(currentScene))
        {
            savedPositions[currentScene] = transform.position;
            savedRotations[currentScene] = transform.rotation;
        }

        SceneManager.activeSceneChanged -= OnSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!string.IsNullOrEmpty(oldScene.name))
        {
            savedPositions[oldScene.name] = transform.position;
            savedRotations[oldScene.name] = transform.rotation;
        }

        currentScene = newScene.name;

        if (savedPositions.ContainsKey(currentScene))
            transform.position = savedPositions[currentScene];

        if (savedRotations.ContainsKey(currentScene))
            transform.rotation = savedRotations[currentScene];
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-enable movement after the scene is fully loaded
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log($"[PlayerPositionSaver] PlayerMovement3D re-enabled in scene {scene.name}");
        }
    }

    public static void ClearSavedData()
    {
        savedPositions.Clear();
        savedRotations.Clear();
    }
}
