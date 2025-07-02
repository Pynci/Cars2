using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class Fase2RedCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;
    public RaceManager raceManager;

    [Header("Rewards (hardcoded)")]
    private const float checkpointReward = 10.0f;
    private const float lapReward = 50.0f;
    private const float timePenalty = -0.1f;
    private const float opponentAheadPenalty = -5.0f;
    private const float opponentBehindReward = 5.0f;
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

    private CarController controller;
    private Rigidbody rb;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private int nextCheckpoint = 0;
    private Transform targetCheckpoint;
    private int completedCheckpoints;
    private float smoothLastDist;
    private float idleTimer = 0f;
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
        idleTimer = 0f;
        completedLap = false;
        isFinishLine = false;

    targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        smoothLastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        Debug.Log(" checkpoint 78787 "+targetCheckpoint);
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

        var oppAgent = opponent.GetComponent<Fase2BlueCarAgent>();
        bool isAhead = completedCheckpoints > (oppAgent != null ? oppAgent.GetCompletedCheckpoints() : 0);
        sensor.AddObservation(isAhead ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);
        controller.Move(accel, steer, brake);

        AddReward(timePenalty * Time.fixedDeltaTime);
        AddReward(-1f / maxStepsPerEpisode);

        float currentDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = smoothingAlpha * currentDist + (1f - smoothingAlpha) * smoothLastDist;
        AddReward((smoothLastDist - currentDist) * progressRewardMultiplier);

        Vector3 toCP = (targetCheckpoint.position - transform.position).normalized;
        if (Vector3.Dot(transform.forward, toCP) > 0.5f)
        {
            AddReward((rb.linearVelocity.magnitude / maxExpectedSpeed) * speedRewardMultiplier * Time.fixedDeltaTime);
        }

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

        if (currentDist < 5f)
        {
            AddReward(checkpointReward / checkpointManager.TotalCheckpoints);
            completedCheckpoints++;
            Debug.Log("attraversato checkpoint");
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;
            
            var oppAgent = opponent.GetComponent<Fase2BlueCarAgent>();
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

            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
            smoothLastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
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
            Debug.Log("Red collided with Blue");
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;

    public float GetProgress() => completedCheckpoints / (float)checkpointManager.TotalCheckpoints;

    public bool HasCompletedLap() => completedLap;
}
