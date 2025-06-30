using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class RedCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform[] spawnPoints;
    public Transform opponent;

    [Header("Rewards")]
    public float checkpointReward = 2f;
    public float lapReward = 10f;
    public float collisionPenalty = -3f;
    public float progressRewardMultiplier = 1f;
    public float speedRewardMultiplier = 0.1f;
    public float idlePenalty = -0.05f;

    private CarController controller;
    private Rigidbody rb;
    private int nextCheckpoint = 0;
    private Transform targetCheckpoint;
    private float lastDist;
    private int completedCheckpoints;
    private Vector3 spawnPoint = new Vector3(-213.1f, 0f, 53f);

    private float idleTimer = 0f;
    private const float idleThreshold = 3f;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        nextCheckpoint = 0;
        completedCheckpoints = 0;
        idleTimer = 0f;

        transform.position = spawnPoint;
        transform.rotation = Quaternion.identity;
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / 20f);
        sensor.AddObservation(localVel.x / 10f);

        float progress = completedCheckpoints / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

        if (opponent)
        {
            float od = Vector3.Distance(transform.position, opponent.position);
            sensor.AddObservation(Mathf.Clamp(od, 0f, 50f) / 50f);
        }
        else sensor.AddObservation(1f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);
        controller.Move(accel, steer, brake);

        // Step penalty
        AddReward(-0.0005f);

        // Progress reward
        float dist = Vector3.Distance(transform.position, targetCheckpoint.position);
        AddReward((lastDist - dist) * progressRewardMultiplier);
        lastDist = dist;

        // Speed reward (solo se nella direzione giusta)
        Vector3 toCheckpoint = (targetCheckpoint.position - transform.position).normalized;
        float dirDot = Vector3.Dot(transform.forward, toCheckpoint);
        if (dirDot > 0.5f)
            AddReward(rb.linearVelocity.magnitude * speedRewardMultiplier * Time.fixedDeltaTime);

        // Idle penalty
        if (rb.linearVelocity.magnitude < 1f)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer > idleThreshold)
            {
                AddReward(idlePenalty);
                idleTimer = 0f;
            }
        }
        else idleTimer = 0f;

        // Checkpoint
        if (dist < 5f)
        {
            AddReward(checkpointReward);
            completedCheckpoints++;
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;
            if (nextCheckpoint == 0)
            {
                AddReward(lapReward);
                completedCheckpoints = 0;
            }
            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
            lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxis("Vertical");
        ca[1] = Input.GetAxis("Horizontal");
        ca[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            transform.position = spawnPoint;
            transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;
}
