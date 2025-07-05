using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public float progressReward = 1f;
    private Dictionary<CarAgent, int> nextIndex = new Dictionary<CarAgent, int>();

    public int GetCheckpointIndex(CarAgent agent)
    {
        return nextIndex[agent];
    }

    public Transform GetNextCheckpoint(CarAgent agent)
    {
        if (!nextIndex.ContainsKey(agent))
            nextIndex[agent] = 0;
        int idx = nextIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    public void HandleCheckpoint(CarAgent agent, float reward)
    {
        int idx = nextIndex.ContainsKey(agent) ? nextIndex[agent] : 0;
        Transform cp = checkpoints[idx];

        Vector3 toCP = (cp.position - agent.transform.position).normalized;
        if (Vector3.Dot(agent.transform.forward, toCP) > 0.5f)
        {
            agent.AddReward(reward);
        }
        if (Vector3.Distance(agent.transform.position, checkpoints[idx % checkpoints.Length].position) < 2f)
        {
            agent.AddReward(progressReward);
            nextIndex[agent] = (idx + 1) % checkpoints.Length;
        }
    }
}