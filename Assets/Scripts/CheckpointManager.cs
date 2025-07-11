using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public float progressReward = 2f;
    public float checkpointReachedReward = 3f;
    public const float undercutPenalty = -2f;
    public int TotalCheckpoints => checkpoints.Length;
    private Dictionary<RedCarAgent, int> RedcurrentIndex = new Dictionary<RedCarAgent, int>();
    private Dictionary<BlueCarAgent, int> BluecurrentIndex = new Dictionary<BlueCarAgent, int>();

    public Transform GetNextCheckpoint(int index)
    {
        return checkpoints[index];
    }
    public int GetCurrentCheckpointIndex(RedCarAgent agent)
    {
        if (!RedcurrentIndex.TryGetValue(agent, out var idx))
            RedcurrentIndex[agent] = idx = 0;
        return idx;
    }

    public int GetCurrentCheckpointIndex(BlueCarAgent agent)
    {
        if (!BluecurrentIndex.TryGetValue(agent, out var idx))
            BluecurrentIndex[agent] = idx = 0;
        return idx;
    }

    public Transform GetCurrentCheckpoint(RedCarAgent agent)
    {
        if (!RedcurrentIndex.ContainsKey(agent))
            RedcurrentIndex[agent] = 0;
        int idx = RedcurrentIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    public Transform GetCurrentCheckpoint(BlueCarAgent agent)
    {
        if (!BluecurrentIndex.ContainsKey(agent))
            BluecurrentIndex[agent] = 0;
        int idx = BluecurrentIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    public (Transform cp, int idx) DetectNextCheckpointWithIndex(RedCarAgent agent)
    {
        Transform bestCp = null;
        int bestIdx = -1;
        float bestScore = float.MaxValue;
        Vector3 forward = agent.transform.forward;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            var cp = checkpoints[i];
            Vector3 toCp = cp.position - agent.transform.position;
            float dot = Vector3.Dot(forward, toCp.normalized);
            if (dot < 0.3f) continue;

            float score = toCp.sqrMagnitude / dot;
            if (score < bestScore)
            {
                bestScore = score;
                bestCp = cp;
                bestIdx = i;
            }
        }

        // Fallback: usa l’indice “storico” se non trova nulla di fronte
        if (bestCp == null)
        {
            int historical = RedcurrentIndex.ContainsKey(agent)
                ? (RedcurrentIndex[agent] + 1) % checkpoints.Length
                : 0;
            bestCp = checkpoints[historical];
            bestIdx = historical;
        }

        return (bestCp, bestIdx);
    }

    public (Transform cp, int idx) DetectNextCheckpointWithIndex(BlueCarAgent agent)
    {
        Transform bestCp = null;
        int bestIdx = -1;
        float bestScore = float.MaxValue;
        Vector3 forward = agent.transform.forward;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            var cp = checkpoints[i];
            Vector3 toCp = cp.position - agent.transform.position;
            float dot = Vector3.Dot(forward, toCp.normalized);
            if (dot < 0.3f) continue;

            float score = toCp.sqrMagnitude / dot;
            if (score < bestScore)
            {
                bestScore = score;
                bestCp = cp;
                bestIdx = i;
            }
        }

        // Fallback: usa l’indice “storico” se non trova nulla di fronte
        if (bestCp == null)
        {
            int historical = BluecurrentIndex.ContainsKey(agent)
                ? (BluecurrentIndex[agent] + 1) % checkpoints.Length
                : 0;
            bestCp = checkpoints[historical];
            bestIdx = historical;
        }

        return (bestCp, bestIdx);
    }

    public void EvaluateCheckpointProgress(RedCarAgent agent, int detectedIdx)
    {
        int idx = GetCurrentCheckpointIndex(agent);
        var cp = checkpoints[detectedIdx];
        Vector3 toCp = (cp.position - agent.transform.position).normalized;
        float facing = Vector3.Dot(agent.transform.forward, toCp);

        // 1) reward/penalità base per direzione
        if (facing > 0.5f)
            agent.AddReward(progressReward);
        else
            agent.AddReward(-progressReward);  // penalità se guarda lontano dal cp

        // 2) se attraversa correttamente
        if (detectedIdx == idx && Vector3.Distance(agent.transform.position, cp.position) < 2f)
        {
            agent.AddReward(checkpointReachedReward);
            RedcurrentIndex[agent] = (idx + 1) % checkpoints.Length;
        }
        // 3) se “geometricamente” davanti, ma id diverso → overtake
        else if (detectedIdx > idx)
        {
            agent.AddReward(progressReward);
        }
        // 4) se geometr. indietro → penalità
        else if (detectedIdx < idx)
        {
            agent.AddReward(undercutPenalty);
        }
    }

    public void EvaluateCheckpointProgress(BlueCarAgent agent, int detectedIdx)
    {
        int idx = GetCurrentCheckpointIndex(agent);
        var cp = checkpoints[detectedIdx];
        Vector3 toCp = (cp.position - agent.transform.position).normalized;
        float facing = Vector3.Dot(agent.transform.forward, toCp);

        // 1) reward/penalità base per direzione
        if (facing > 0.5f)
            agent.AddReward(progressReward);
        else
            agent.AddReward(-progressReward);  // penalità se guarda lontano dal cp

        // 2) se attraversa correttamente
        if (detectedIdx == idx && Vector3.Distance(agent.transform.position, cp.position) < 2f)
        {
            agent.AddReward(checkpointReachedReward);
            BluecurrentIndex[agent] = (idx + 1) % checkpoints.Length;
        }
        // 3) se “geometricamente” davanti, ma id diverso → overtake
        else if (detectedIdx > idx)
        {
            agent.AddReward(progressReward);
        }
        // 4) se geometr. indietro → penalità
        else if (detectedIdx < idx)
        {
            agent.AddReward(undercutPenalty);
        }
    }

}