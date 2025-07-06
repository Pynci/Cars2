
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
    public int nextCheckpointIndex;

    [Header("Rewards (hardcoded)")]
    private const float timePenalty = -0.1f;
    private const float collisionPenalty = -20.0f;
    private const float opponentCollisionPenalty = -1.0f;
    private const float progressRewardMultiplier = 1.0f;


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
        raceManager = FindFirstObjectByType<RaceManager>();
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

        var (cp, idx) = checkpointManager.DetectNextCheckpointWithIndex(this);
        nextCheckpoint = cp;
        nextCheckpointIndex = idx;      // aggiorna anche il “contatore” interno
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
        if (nextCheckpoint == null) Debug.LogError($"{name}: nextCheckpoint è null!");
        if (checkpointManager == null) Debug.LogError($"{name}: checkpointManager è null!");
        if (raceManager == null) Debug.LogError($"{name}: raceManager è null!");
        float motor = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        controller.Move(motor, steer, brake);
        AddReward(-0.0005f);

        AddReward(timePenalty * Time.fixedDeltaTime);

        float currentDist = Vector3.Distance(transform.position, nextCheckpoint.position);
        smoothLastDist = smoothingAlpha * currentDist + (1f - smoothingAlpha) * smoothLastDist;
        AddReward((smoothLastDist - currentDist) * progressRewardMultiplier);
        lastDist = currentDist;

        checkpointManager.EvaluateCheckpointProgress(this, nextCheckpointIndex);

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
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            raceManager.ResetAllAgents();
        }
        else if (collision.collider.CompareTag("Car"))
        {
            AddReward(opponentCollisionPenalty);
        }
    }
}