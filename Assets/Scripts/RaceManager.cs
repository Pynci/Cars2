using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Splines;

public class RaceManager : MonoBehaviour
{
    public SpawnManager spawnManager;
    public CheckpointManager checkpointManager;
    private CarAgent[] agents;

    public float positionReward = 0.5f; // premio per chi è davanti
    public float positionPenalty = -0.2f; // penalità per chi è indietro
    public float maxLapCompletedReward = 20f; // premio completamento lap
    public float racePenalty = -5f; //penalità per aver perso la gara

    void Start()
    {
        SetupRace();
    }

    public void SetupRace()
    {
        spawnManager.SetupEpisode();
        agents = spawnManager.GetSpawnedAgents().ToArray();
        foreach (var agent in agents)
            agent.SetRaceManager(this);
    }

    public void SetupRaceManager(CarAgent agent)
    {
        agent.SetRaceManager(this);
    }

    public void ResetAllAgents()
    {
        SetupRace();
        foreach (var agent in agents)
            agent.EndEpisode();
    }

    public void UpdateRaceProgress()
    {
        // Solo se siamo nella fase di gara, applica la logica di posizione
        
        // Ordina gli agenti: prima per checkpoint superati, poi per distanza residua dal prossimo
        var ordered = agents.OrderByDescending(agent =>
        {
            var (checkpoint, checkpointIndex) = checkpointManager.DetectNextCheckpointWithIndex(agent);
            int index = checkpointIndex;
            float distanceToNext = Vector3.Distance(agent.transform.position, checkpoint.position);
            return checkpointIndex * 1000f - distanceToNext;  // più checkpoint = meglio
        }).ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            CarAgent agent = ordered[i];

            // Primo classificato → reward, ultimi → penalità proporzionale alla posizione
            float reward = Mathf.Lerp(positionReward, positionPenalty, (float)i / (ordered.Count - 1));
            agent.AddReward(reward * Time.fixedDeltaTime);
        }
        
    }


    public Transform RespawnAgent()
    {
        // Trova posizione libera
        var usedPositions = spawnManager.GetSpawnedAgents()
            .Select(a => a.transform.position)
            .ToHashSet();

        List<Transform> availablePositions = null;

        if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            availablePositions = spawnManager.gridPositions
                .Where(pos => !usedPositions.Contains(pos.position))
                .ToList();
        }
        else if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.RandomSpawn)
        {
             availablePositions = spawnManager.randomPositions
                .Where(pos => !usedPositions.Contains(pos.position))
                .ToList();
        }

        if (availablePositions.Count == 0) Debug.Log("Non ci sono posizioni disponibili"); // Nessuna posizione disponibile

        Transform newSpawn = availablePositions[Random.Range(0, availablePositions.Count)];

        return newSpawn;
    }



    public void NotifyMaxLapReached(CarAgent winnerAgent)
    {
        Debug.Log("in race manager max lap");
        if (winnerAgent == null) return;

        winnerAgent.AddReward(maxLapCompletedReward);

        foreach (var agent in agents)
        {
            if (agent != winnerAgent)
            {
                Debug.Log("end episode ");
                agent.AddReward(racePenalty);
            }
            agent.EndEpisode();
        }
    }

}
