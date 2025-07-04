using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public float progressReward = 1f;

    // Tiene traccia dell'indice del prossimo checkpoint per ogni agente
    private Dictionary<CarAgent, int> agentNextIndex = new Dictionary<CarAgent, int>();

    // Restituisce il checkpoint successivo e aggiorna l'indice ciclicamente
    public Transform GetNextCheckpoint(CarAgent agent)
    {
        if (!agentNextIndex.ContainsKey(agent))
            agentNextIndex[agent] = 0;

        int idx = agentNextIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    // Chiamato dall'agente quando entra nel trigger di un checkpoint
    public void HandleCheckpoint(CarAgent agent)
    {
        int idx = agentNextIndex.ContainsKey(agent) ? agentNextIndex[agent] : 0;
        Transform target = checkpoints[idx % checkpoints.Length];

        // Verifica che sia il checkpoint giusto
        if (Vector3.Distance(agent.transform.position, target.position) < 1f)
        {
            agent.AddRewardComponent(progressReward);
            agentNextIndex[agent] = (idx + 1) % checkpoints.Length;
        }
    }
}