using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    /*
    public GameObject environmentPrefab;
    public int numEnvironments = 4;
    public int environmentsPerRow = 2;

    [Tooltip("Spaziatura tra le colonne (asse X)")]
    public float columnSpacing = 150f;

    [Tooltip("Spaziatura tra le righe (asse Z)")]
    public float rowSpacing = 60f;

    void Start()
    {
        if (environmentPrefab == null)
        {
            Debug.LogError("Environment prefab is not assigned!");
            return;
        }

        for (int i = 0; i < numEnvironments; i++)
        {
            int row = i / environmentsPerRow;
            int col = i % environmentsPerRow;

            Vector3 position = new Vector3(col * columnSpacing, 0f, row * rowSpacing);

            GameObject env = Instantiate(environmentPrefab, position, Quaternion.identity);
            env.name = "Environment_" + i;
        }
    }
    */
}
