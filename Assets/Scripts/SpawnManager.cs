using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public GameObject carPrefab;
    public Transform[] gridPositions;
    public Transform[] randomPositions;

    [Tooltip("Scegli '0' per Random, '1' per Grid")]
    public int spawnMode = 1;

    [Tooltip("Numero di agenti da instanziare")]
    public int agentCount = 2;

    private List<GameObject> spawnedAgents = new List<GameObject>();

    public void SetupEpisode()
    {
        // Pulisci episodio precedente
        foreach (var agent in spawnedAgents)
            Destroy(agent);
        spawnedAgents.Clear();

        // Scegli array di posizioni
        Transform[] positions = (spawnMode == 0) ? randomPositions : gridPositions;

        for (int i = 0; i < agentCount; i++)
        {
            Vector3 pos = positions[i % positions.Length].position;
            Quaternion rot = positions[i % positions.Length].rotation;
            var agent = Instantiate(carPrefab, pos, rot);
            spawnedAgents.Add(agent);
        }
    }
}