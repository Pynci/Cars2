using System.Collections.Generic;
using UnityEngine;
using static SpawnManager;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public float progressReward = 0.1f;
    public float checkpointReachedReward = 2f;
    public const float undercutPenalty = -2f;
    public int TotalCheckpoints => checkpoints.Length;
    private Dictionary<CarAgent, int> currentIndex = new Dictionary<CarAgent, int>();

    public int GetCurrentCheckpointIndex(CarAgent agent)
    {
        //questo serve perchè prima dello spawn darebbe errore
        if (!currentIndex.TryGetValue(agent, out var idx))
            currentIndex[agent] = idx = 0;
        return idx;
    }

    public Transform GetCurrentCheckpoint(CarAgent agent)
    {
        if (!currentIndex.ContainsKey(agent))
            currentIndex[agent] = 0;
        int idx = currentIndex[agent];
        return checkpoints[idx % checkpoints.Length];
    }

    public (Transform cp, int idx) DetectNextCheckpointWithIndex(CarAgent agent)
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
            int historical = currentIndex.ContainsKey(agent)
                ? (currentIndex[agent] + 1) % checkpoints.Length
                : 0;
            bestCp = checkpoints[historical];
            bestIdx = historical;
        }

        return (bestCp, bestIdx);
    }

    public void EvaluateCheckpointProgress(CarAgent agent, TrainingPhase raceMode)
    {
        int idx = GetCurrentCheckpointIndex(agent);
        //Debug.Log(" idx: " + idx);
        var (detectedCP, detectedIdx) = DetectNextCheckpointWithIndex(agent);
        //Debug.Log("detected idx: " + detectedIdx);
        //var cp = checkpoints[detectedIdx];
        
        Vector3 toCp = (detectedCP.position - agent.transform.position).normalized;
        float facing = Vector3.Dot(agent.transform.forward, toCp);

        // 1) reward/penalità base per direzione
        if (facing > 0.5f)
            agent.AddReward(progressReward);
        else
            agent.AddReward(-progressReward);  // penalità se guarda lontano dal cp

        int nextIdx = (idx + 1) % checkpoints.Length;
        // 2) se attraversa correttamente
        if (detectedIdx == nextIdx && Vector3.Distance(agent.transform.position, detectedCP.position) < 8f)
        {
            agent.AddReward(checkpointReachedReward);
            currentIndex[agent] = nextIdx;
            //Debug.Log("detected checkpoint: "+detectedIdx+" current idx: " + currentIndex[agent]);
            if (detectedIdx == 0 && raceMode == SpawnManager.TrainingPhase.Race)
            {
                //Debug.Log("add lap checkpoint");
                agent.AddLap();
            }
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