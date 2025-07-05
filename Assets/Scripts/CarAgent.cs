// CarAgent.cs
using System.ComponentModel;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
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

    [Header("Rewards (hardcoded)")]
    private const float checkpointReward = 10.0f;
    private const float timePenalty = -0.1f;
    private const float collisionPenalty = -20.0f;
    private const float opponentCollisionPenalty = -1.0f;
    private const float progressRewardMultiplier = 1.0f;
    private const float speedRewardMultiplier = 0.1f;

    [Header("Normalization")]
    private int maxStepsPerEpisode = 300;
    private float maxExpectedSpeed = 40f;

    [Header("Idle Timeout")]
    private float maxIdleTime = 5f;

    [Header("Progress Smoothing")]
    [Range(0f, 1f)] public float smoothingAlpha = 0.2f;

    private int completedCheckpoints;
    private float lastDist;
    private float smoothLastDist;
    private float idleTimer = 0f;

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

        AddReward(timePenalty * Time.fixedDeltaTime);
        AddReward(-1f / maxStepsPerEpisode);

        float currentDist = Vector3.Distance(transform.position, nextCheckpoint.position);
        smoothLastDist = smoothingAlpha * currentDist + (1f - smoothingAlpha) * smoothLastDist;
        AddReward((smoothLastDist - currentDist) * progressRewardMultiplier);
        lastDist = currentDist;

        float reward = (rb.linearVelocity.magnitude / maxExpectedSpeed)*speedRewardMultiplier * Time.fixedDeltaTime;
        checkpointManager.HandleCheckpoint(this, reward);

        if (rb.linearVelocity.magnitude < 1f)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer > maxIdleTime)
            {
                AddReward(collisionPenalty);
                EndEpisode();
            }
        }
        else
        {
            idleTimer = 0f;
        }

        raceManager.UpdateRaceProgress();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetAxis("Vertical");
        cont[1] = Input.GetAxis("Horizontal");
        cont[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("bulkheads"))
        {
            AddReward(-1f);
            raceManager.ResetAllAgents();
        }
        else if (collision.collider.CompareTag("agent"))
        {
            AddReward(opponentCollisionPenalty);
        }
    }
}