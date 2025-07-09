using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public SpawnManager spawnManager;
    public CheckpointManager checkpointManager;
    private CarAgent[] agents;

    public float positionReward = 0.05f; // premio per chi è davanti
    public float positionPenalty = -0.02f; // penalità per chi è indietro

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
        if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
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
    }

    public void notifyCrash(CarAgent agent)
    {
        if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.Race)
        {
            Destroy(agent);
            Destroy(agent.gameObject);
        }
        else if (spawnManager.trainingPhase == SpawnManager.TrainingPhase.RandomSpawn)
        {
            agent.EndEpisode();
            RespawnAgent(agent);
        }
    }

    public void RespawnAgent(CarAgent crashedAgent)
    {
        var behavior = crashedAgent.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>()?.BehaviorName;
        if (string.IsNullOrEmpty(behavior)) return;

        // Trova posizione libera
        var usedPositions = spawnManager.GetSpawnedAgents()
            .Select(a => a.transform.position)
            .ToHashSet();

        var availablePositions = spawnManager.randomPositions
            .Where(pos => !usedPositions.Contains(pos.position))
            .ToList();

        if (availablePositions.Count == 0) return; // Nessuna posizione disponibile

        var newSpawn = availablePositions[Random.Range(0, availablePositions.Count)];

        int i = agents.Length;
        bool found = false;
        while (!found)
        {
            if (agents[i].Equals(crashedAgent))
                found = true;
            else i--;
        }

        CarAgent newAgent = spawnManager.RespawnAgent(newSpawn, behavior);
        newAgent.SetRaceManager(this);
        agents[i] = newAgent;
    }

}
