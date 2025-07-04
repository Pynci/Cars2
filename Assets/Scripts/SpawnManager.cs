using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SpawnManager : MonoBehaviour
{
    public GameObject carPrefab;
    public Transform[] gridPositions;
    public Transform[] randomPositions;

    [Tooltip("Scegli '0' per Random, '1' per Grid")]
    public int spawnMode = 1;

    [Tooltip("Numero di agenti da instanziare")]
    public int agentCount = 2;

    [Tooltip("Lista di materiali predefiniti per differenziare le auto")]
    public Material[] availableMaterials;

    private List<GameObject> spawnedAgents = new List<GameObject>();

    public void SetupEpisode()
    {
        // Pulisci episodio precedente
        foreach (var agent in spawnedAgents)
            Destroy(agent);
        spawnedAgents.Clear();

        if (spawnMode == 0)
        {
            // Random: scegli agentCount posizioni uniche in modo casuale
            var sampled = randomPositions
                .OrderBy(_ => Random.value)
                .Take(agentCount)
                .ToArray();
            foreach (var t in sampled)
            {
                InstantiateAgentAt(t);
            }
        }
        else
        {
            // Grid: prendi i primi agentCount punti in ordine
            for (int i = 0; i < agentCount; i++)
            {
                var t = gridPositions[i % gridPositions.Length];
                InstantiateAgentAt(t);
            }
        }
    }

    private void InstantiateAgentAt(Transform spawnPoint)
    {
        var agent = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);

        // Assegna colormap diversa al GameObject "body"
        Transform bodyTransform = agent.transform.Find("raceRed/body");
        if (bodyTransform != null)
        {
            Renderer rend = bodyTransform.GetComponent<Renderer>();
            if (rend != null && spawnedAgents.Count < availableMaterials.Length)
            {
                rend.material = new Material(availableMaterials[spawnedAgents.Count]);
            }
            else
            {
                Debug.LogWarning("Colormap non assegnata: indice fuori dai limiti o renderer mancante.");
            }
        }

        spawnedAgents.Add(agent);
    }

}