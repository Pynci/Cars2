using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject carPrefab;
    public Transform[] gridPositions;
    public Transform[] randomPositions;
    public Transform[] usedPositions;

    public GameObject[] spawnPrefabs;

    public bool isInference;

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
        if(!isInference)
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

            usedPositions = positions.ToArray();

        } else {
            var positions = (trainingPhase == TrainingPhase.RandomSpawn)
            ? randomPositions.OrderBy(_ => Random.value).Take(agentCount)
            : gridPositions.Take(agentCount);  // Grid spawn nella fase di gara

            Transform[] pos = positions.ToArray();
            int i = 0;

            foreach(GameObject prefab in spawnPrefabs)
            {
                InitializeAgentAt(prefab, pos[i]);
                i++;
            }

            usedPositions = positions.ToArray();
        }
        
    }

    private void InitializeAgentAt(GameObject agent, Transform spawnpoint)
    {
        spawnedAgents.Add(agent);
        agent.transform.position = spawnpoint.position;
        agent.transform.rotation = spawnpoint.rotation;
    }


    private void InstantiateAgentAt(Transform spawnPoint)
    {
        var agentObj = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        var behaviorParameters = agentObj.GetComponent<BehaviorParameters>();
        string Newbehavior = null;

   
        spawnedAgents.Add(agentObj);

        // Assegna il comportamento
        if (behaviorParameters != null && spawnedAgents.Count <= agentBehaviors.Length)
        {
            Newbehavior = agentBehaviors[spawnedAgents.Count - 1];
            behaviorParameters.BehaviorName = Newbehavior;
        }

        // Applica materiale
        if (spawnedAgents.Count <= availableMaterials.Length)
        {
            var body = agentObj.transform.Find("raceCar/body");
            if (Newbehavior != null && body != null && body.TryGetComponent<Renderer>(out var rend))
            {
                Material material = new Material(availableMaterials[spawnedAgents.Count - 1]);
                rend.material = material;
            }
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
