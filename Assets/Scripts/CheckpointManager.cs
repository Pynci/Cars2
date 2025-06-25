using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public Transform[] checkpoints;

    public Transform GetNextCheckpoint(int index)
    {
        return checkpoints[index % checkpoints.Length];
    }
}
