using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Policies;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject carPrefab;
    public Transform[] gridPositions;
    public Transform[] randomPositions;

    [Tooltip("Numero di agenti da instanziare")]
    public int agentCount = 2;
    [Tooltip("Lista di materiali predefiniti per differenziare le auto")]
    public Material[] availableMaterials;

    // Nuova variabile per tracciare la fase di addestramento
    public enum TrainingPhase { RandomSpawn, Race }
    public TrainingPhase trainingPhase = TrainingPhase.RandomSpawn;

    private List<GameObject> spawnedAgents = new List<GameObject>();
    public string[] agentBehaviors = { "BlueCar", "RedCar", "GreenCar", "RoseCar", "YellowCar", "VioletCar" };

    public void SetupEpisode()
    {
        // Distrugge gli agenti precedenti
        foreach (var agent in spawnedAgents)
            Destroy(agent);
        spawnedAgents.Clear();

        // Seleziona posizioni in base alla fase di addestramento
        var positions = (trainingPhase == TrainingPhase.RandomSpawn)
            ? randomPositions.OrderBy(_ => Random.value).Take(agentCount)
            : gridPositions.Take(agentCount);  // Grid spawn nella fase di gara

        // Instanzia agenti
        foreach (var spawnPoint in positions)
            InstantiateAgentAt(spawnPoint);
    }

    private void InstantiateAgentAt(Transform spawnPoint)
    {
        var agentObj = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        var behaviorParameters = agentObj.GetComponent<BehaviorParameters>();
        spawnedAgents.Add(agentObj);

        // Applica materiale
        if (spawnedAgents.Count <= availableMaterials.Length)
        {
            var body = agentObj.transform.Find("raceCar/body");
            if (body != null && body.TryGetComponent<Renderer>(out var rend))
                rend.material = new Material(availableMaterials[spawnedAgents.Count - 1]);
        }

        // Assegna il comportamento
        if (behaviorParameters != null && spawnedAgents.Count <= agentBehaviors.Length)
        {
            behaviorParameters.BehaviorName = agentBehaviors[spawnedAgents.Count - 1];
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

    public int getSpawnMode()
    {
        return trainingPhase == TrainingPhase.RandomSpawn ? 0 : 1;  // 0 per spawn casuale, 1 per grid spawn
    }
}
