using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarAgent : Agent
{
    private Rigidbody rb;
    private float episodeReward;
    private CarController controller;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        episodeReward = 0f;
        FindFirstObjectByType<SpawnManager>().SetupEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        
    }

    public void AddRewardComponent(float reward)
    {
        episodeReward += reward;
        AddReward(reward);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
            FindFirstObjectByType<CheckpointManager>().HandleCheckpoint(this);
    }
}