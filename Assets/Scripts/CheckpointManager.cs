using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public float progressReward = 1f;
    private Dictionary<CarAgent, int> nextIndex = new Dictionary<CarAgent, int>();

    public Transform GetNextCheckpoint(CarAgent agent)
    {
        if (!nextIndex.ContainsKey(agent))
            nextIndex[agent] = 0;
        int idx = nextIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    public void HandleCheckpoint(CarAgent agent)
    {
        int idx = nextIndex.ContainsKey(agent) ? nextIndex[agent] : 0;
        if (Vector3.Distance(agent.transform.position, checkpoints[idx % checkpoints.Length].position) < 2f)
        {
            agent.AddReward(progressReward);
            nextIndex[agent] = (idx + 1) % checkpoints.Length;
        }
    }
}