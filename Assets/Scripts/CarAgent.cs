// CarAgent.cs
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarAgent : Agent
{
    private Rigidbody rb;
    private CarController controller;
    private CheckpointManager checkpointManager;
    private RaceManager raceManager;

    [Header("Agent Settings")]
    public float maxSpeed = 20f;
    public Transform nextCheckpoint;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
    }

    // Metodo chiamato da RaceManager per assegnare il riferimento
    public void SetRaceManager(RaceManager rm)
    {
        raceManager = rm;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        nextCheckpoint = checkpointManager.GetNextCheckpoint(this);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var dir = (nextCheckpoint.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, nextCheckpoint.position);
        sensor.AddObservation(transform.InverseTransformDirection(dir));
        sensor.AddObservation(dist / 100f);
        sensor.AddObservation(transform.InverseTransformDirection(rb.linearVelocity) / maxSpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float motor = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        controller.Move(motor, steer, brake);
        AddReward(-0.0005f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetAxis("Vertical");
        cont[1] = Input.GetAxis("Horizontal");
        cont[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    // Reward checkpoint
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            AddReward(checkpointManager.progressReward);
            nextCheckpoint = checkpointManager.GetNextCheckpoint(this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(-1f);
            raceManager.ResetAllAgents();
        }
    }
}