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
        // Distrugge gli agenti precedenti
        foreach (var agent in spawnedAgents)
            Destroy(agent);
        spawnedAgents.Clear();

        // Seleziona posizioni
        var positions = (spawnMode == 0)
            ? randomPositions.OrderBy(_ => Random.value).Take(agentCount)
            : gridPositions.Take(agentCount);

        // Instanzia agenti
        foreach (var spawnPoint in positions)
            InstantiateAgentAt(spawnPoint);
    }

    private void InstantiateAgentAt(Transform spawnPoint)
    {
        var agentObj = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedAgents.Add(agentObj);

        // Applica materiale
        if (spawnedAgents.Count <= availableMaterials.Length)
        {
            var body = agentObj.transform.Find("raceCar/body");
            if (body != null && body.TryGetComponent<Renderer>(out var rend))
                rend.material = new Material(availableMaterials[spawnedAgents.Count - 1]);
        }
    }

    // Espone la lista di CarAgent istanziati
    public List<CarAgent> GetSpawnedAgents()
    {
        return spawnedAgents
            .Select(go => go.GetComponent<CarAgent>())
            .Where(agent => agent != null)
            .ToList();
    }

    public int SpawnMode()
    {
        return spawnMode;
    }
}