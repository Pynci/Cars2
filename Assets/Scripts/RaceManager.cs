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

    public void ResetAllAgents()
    {
        SetupRace();
        foreach (var agent in agents)
            agent.EndEpisode();
    }


    public void UpdateRaceProgress()
    {
        if (spawnManager.getSpawnMode() == 1)
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

}
