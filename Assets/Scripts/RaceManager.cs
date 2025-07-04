using UnityEngine;
public class RaceManager : MonoBehaviour
{
    public SpawnManager spawnManager;
    private CarAgent[] agents;

    void Start()
    {
        SetupRace();
    }

    public void SetupRace()
    {
        // Instanzia e ottiene agenti senza FindObjects di scena
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
}
