using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Splines;
using static SpawnManager;

public class RaceManager : MonoBehaviour
{
    public SpawnManager spawnManager;
    public CheckpointManager checkpointManager;
    private CarAgent[] agents;

    public float positionReward = 0.005f; // premio per chi è davanti
    public float positionPenalty = -0.001f; // penalità per chi è indietro
    public float maxLapCompletedReward = 3f; // premio completamento lap
    public float racePenalty = -0.5f; //penalità per aver perso la gara
    public float betterRankReward = 0.1f;



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

    public void UpdateRaceProgress()
    {
        // Ordina gli agenti per progresso
        var ordered = agents.OrderByDescending(agent =>
        {
            var (checkpoint, checkpointIndex) = checkpointManager.DetectNextCheckpointWithIndex(agent);
            float distanceToNext = Vector3.Distance(agent.transform.position, checkpoint.position);
            return checkpointIndex * 1000f - distanceToNext;
        }).ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            CarAgent agent = ordered[i];

            // sorpasso
            if (agent.lastRank != -1 && i < agent.lastRank)
            {
                agent.AddReward(betterRankReward); 
            }

            float reward = Mathf.Lerp(positionReward, positionPenalty, (float)i / (ordered.Count - 1));
            agent.AddReward(reward);

            agent.lastRank = i; // aggiornamento classifica attuale
        }
    }



    public Transform RespawnAgent()
    {
        float distanceThreshold = 5.0f;
        var agents = spawnManager.GetSpawnedAgents();

        List<Transform> availablePositions = null;

        if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            // Prende solo le prime N posizioni definite
            var candidatePositions = spawnManager.gridPositions
                .Take(agents.Count);

            availablePositions = candidatePositions
                .Where(pos =>
                    !agents.Any(agent =>
                        Vector3.Distance(agent.transform.position, pos.position) < distanceThreshold))
                .ToList();
        }
        else if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.RandomSpawn)
        {
            availablePositions = spawnManager.randomPositions
                .Where(pos =>
                    !agents.Any(agent =>
                        Vector3.Distance(agent.transform.position, pos.position) < distanceThreshold))
                .ToList();
        }

        if (availablePositions == null || availablePositions.Count == 0)
        {
            Debug.LogWarning("Non ci sono posizioni disponibili per il respawn.");
            return null;
        }

        return availablePositions[Random.Range(0, availablePositions.Count)];
    }



    public void NotifyMaxLapReached(CarAgent winnerAgent)
    {
        if (winnerAgent == null) return;

        winnerAgent.AddReward(maxLapCompletedReward);


        foreach (var agent in agents)
        {
            if (agent != winnerAgent)
            {
                agent.AddReward(racePenalty);
            }

            agent.SetIsRespawn(true);
            agent.EndEpisode();
        }
    }

}
