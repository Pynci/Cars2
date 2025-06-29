using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public int TotalCheckpoints => checkpoints.Length;

    public Transform GetNextCheckpoint(int index)
    {
        return checkpoints[index];
    }

    public Transform GetPreviousCheckpoint(int index)
    {
        if (index > 0)
        {
            return checkpoints[index - 1];
        } else
        {
            return checkpoints[index];
        }
    }
}
