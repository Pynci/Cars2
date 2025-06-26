using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;
    public int TotalCheckpoints => checkpoints.Length;

    public Transform GetNextCheckpoint(int index)
    {
        return checkpoints[index];
    }
}
