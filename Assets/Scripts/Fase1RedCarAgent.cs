using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class Fase1RedCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;

    [Header("Rewards (hardcoded)")]
    private const float checkpointReward = 10.0f;
    private const float timePenalty = -0.1f;
    private const float collisionPenalty = -20.0f;
    private const float opponentCollisionPenalty = -1.0f;
    private const float progressRewardMultiplier = 1.0f;
    private const float speedRewardMultiplier = 0.1f;

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
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //Spawn casuale:
        int startIndex;
        Vector3 spawnPos;

        do
        {
            startIndex = Random.Range(0, checkpointManager.TotalCheckpoints);
            Transform startCp = checkpointManager.GetNextCheckpoint(startIndex);
            spawnPos = startCp.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            spawnPos.y = 0;
            Debug.Log(Physics.CheckSphere(spawnPos, 10f));
        } while (!Physics.CheckSphere(spawnPos, 10f));

        transform.position = spawnPos;

        nextCheckpoint = (startIndex + 1) % checkpointManager.TotalCheckpoints;
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = lastDist;
        completedCheckpoints = 0;
        idleTimer = 0f;

        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        float baseAng = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, baseAng + Random.Range(-10f, 10f), 0f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direzione verso il checkpoint
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        // Velocità localizzata
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxExpectedSpeed);
        sensor.AddObservation(localVel.x / (maxExpectedSpeed * 0.5f));

        // Progresso sui checkpoint
        float progress = completedCheckpoints / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

        // Informazione relativa all'avversario
        var oppAgent = opponent.GetComponent<Fase1BlueCarAgent>();
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
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;
            
            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
            lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
            smoothLastDist = lastDist;
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
}
