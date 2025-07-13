using NUnit.Framework;
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
    public bool isInference;

    public float positionReward = 0.05f; // premio per chi è davanti
    public float positionPenalty = -0.01f; // penalità per chi è indietro
    public float maxLapCompletedReward = 2f; // premio completamento lap
    public float racePenalty = -1.0f; //penalità per aver perso la gara
    public float betterRankReward = 0.2f;
    

    void Start()
    {
        if(!isInference)
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
            agent.AddReward(reward * Time.fixedDeltaTime);

            agent.lastRank = i; // aggiornamento classifica attuale
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
            if (spawnManager.GetSpawnedAgents().Count == 2)
            {
                // Usa solo i primi due slot della griglia
                availablePositions = spawnManager.gridPositions
                    .Take(2)
                    .Where(pos => !usedPositions.Contains(pos.position))
                    .ToList();
            }
            else
            {
                // Usa tutta la griglia come fallback
                availablePositions = spawnManager.gridPositions
                    .Where(pos => !usedPositions.Contains(pos.position))
                    .ToList();
            }
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
        if (winnerAgent == null) return;

        winnerAgent.AddReward(maxLapCompletedReward);

        //agents.Where(agent => agent != winnerAgent)

        foreach (var agent in agents)
        {
            if (agent != winnerAgent)
            {
                agent.AddReward(racePenalty);
            }

            agent.setIsRespawn(true);
            agent.EndEpisode();
        }
    }

}
