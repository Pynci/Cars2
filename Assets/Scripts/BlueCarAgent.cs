using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class BlueCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;

    [Header("Rewards")]
    public float checkpointReward = 2f;
    public float lapReward = 10f;
    public float collisionPenalty = -3f;
    public float opponentCollisionPenalty = -3f;
    public float progressRewardMultiplier = 1f;
    public float speedRewardMultiplier = 0.1f;
    public float idlePenalty = -0.05f;

    [Header("Normalization")]
    public int maxStepsPerEpisode = 300;
    public int maxLaps = 1;
    public float maxExpectedSpeed = 40f;


    [Header("Idle Timeout")]
    public float maxIdleTime = 20f;

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
    private float smoothLastDist;

    private float idleTimer = 0f;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("BlueCar Episode Start");
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        nextCheckpoint = 0;
        completedCheckpoints = 0;
        idleTimer = 0f;

        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = lastDist;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direzione verso il checkpoint successivo
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        // Velocita locale
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxExpectedSpeed);
        sensor.AddObservation(localVel.x / (maxExpectedSpeed * 0.5f));

        // Progresso sul tracciato
        float progress = completedCheckpoints / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);
        controller.Move(accel, steer, brake);

        AddReward(-1f / maxStepsPerEpisode);

        float currentDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = smoothingAlpha * currentDist + (1f - smoothingAlpha) * smoothLastDist;
        AddReward((smoothLastDist - currentDist) * progressRewardMultiplier);
        lastDist = currentDist;

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
                AddReward(idlePenalty * 5f);
                EndEpisode();
            }
        }
        else idleTimer = 0f;

        if (currentDist < 5f)
        {
            AddReward(checkpointReward / checkpointManager.TotalCheckpoints);
            completedCheckpoints++;
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;

            if (nextCheckpoint == 0)
            {
                AddReward(lapReward / maxLaps);
                EndEpisode();
            }

            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
            lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
            smoothLastDist = lastDist;
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
            EndEpisode();
        }
        else if (col.transform == opponent)
        {
            AddReward(opponentCollisionPenalty);
            Debug.Log("Blue collided with Red");
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;
}
