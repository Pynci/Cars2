using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class Fase2BlueCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;
    public RaceManager raceManager;

    
    [Header("Rewards (hardcoded)")]
    private const float lapReward = 50.0f;
    private const float opponentAheadPenalty = -5.0f;
    private const float opponentBehindReward = 5.0f;
    private const float continuousReward = 0.005f;
    private const float checkpointReward = 10f;
    private const float progressRewardMultiplier = 0.2f;
    private const float collisionPenalty = -10.0f;
    private const float opponentCollisionPenalty = -10.0f;
    private const float speedRewardMultiplier = 0.05f;
    private const float slownessPenaltyMultiplier = -0.02f;


    [Header("Normalization")]
    private int maxStepsPerEpisode = 300;
    private float maxExpectedSpeed = 40f;

    [Header("Idle Timeout")]
    private float maxIdleTime = 5f;

    [Header("Progress Smoothing")]
    [Range(0f, 1f)] public float smoothingAlpha = 0.2f;

    private CarController controller;
    private Rigidbody rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private int nextCheckpoint = 0;
    private Transform targetCheckpoint;
    private int completedCheckpoints;
    private float lastDist;
    private bool completedLap = false;
    private bool isFinishLine = false;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        nextCheckpoint = 0;
        completedCheckpoints = 0;
        completedLap = false;
        isFinishLine = false;

        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxExpectedSpeed);
        sensor.AddObservation(localVel.x / (maxExpectedSpeed * 0.5f));

        float progress = completedCheckpoints / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

        var oppAgent = opponent.GetComponent<Fase2RedCarAgent>();
        bool isAhead = completedCheckpoints > (oppAgent != null ? oppAgent.GetCompletedCheckpoints() : 0);
        sensor.AddObservation(isAhead ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);

        // Controllo validità
        if (!float.IsFinite(accel) || !float.IsFinite(steer) || !float.IsFinite(brake)) return;

        controller.Move(accel, steer, brake);

        // Sistema di ricompense

        // Ricompensa base per rimanere attivo
        AddReward(continuousReward);

        // Ricompensa per avvicinamento al checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        float newDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDist - newDistance;
        AddReward(delta * progressRewardMultiplier);
        lastDist = newDistance;

        // Ricompensa per raggiungimento checkpoint
        if (newDistance < 5f)
        {
            AddReward(checkpointReward);
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;
           
            var oppAgent = opponent.GetComponent<Fase2RedCarAgent>();
            if (oppAgent != null && completedCheckpoints > oppAgent.GetCompletedCheckpoints())
                AddReward(opponentBehindReward);
            else if (oppAgent != null && completedCheckpoints < oppAgent.GetCompletedCheckpoints())
                AddReward(opponentAheadPenalty);

            if (nextCheckpoint == 1 && isFinishLine)
            {
                AddReward(lapReward);
                completedLap = true;
                raceManager.NotifyLapCompleted(this);
            }
            else 
            {
                isFinishLine = true;
            }
            
        }

        // Ricompensa per velocità (incoraggia movimento)
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed > 0.1f)
            AddReward(speed * speedRewardMultiplier);
        else
            AddReward(slownessPenaltyMultiplier); // Piccola penalità per lentezza
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.LeftArrow) ? -1f : Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            EndEpisode(); // solo questa macchina
        }
        else if (col.transform == opponent)
        {
            AddReward(opponentCollisionPenalty);
            EndEpisode();
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;

    public float GetProgress() => completedCheckpoints / (float)checkpointManager.TotalCheckpoints;

    public bool HasCompletedLap() => completedLap;
}