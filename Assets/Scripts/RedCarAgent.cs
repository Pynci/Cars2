using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class RedCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;

    [Header("Rewards (hardcoded)")]
    // Valori hardcoded per evitare override dall'Inspector
    private const float checkpointReward = 10.0f;      // Maggior incentivo al progresso
    private const float lapReward = 50.0f;             // Reward consistente per completamento lap
    private const float timePenalty = -0.1f;           // Penalizza tempo in pista
    private const float opponentAheadPenalty = -5.0f;  // Penalità se dietro all'avversario
    private const float opponentBehindReward = 5.0f;    // Ricompensa se supera l'avversario
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
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private int nextCheckpoint = 0;
    private Transform targetCheckpoint;
    private int completedCheckpoints;
    private float lastDist;
    private float smoothLastDist;
    private float idleTimer = 0f;
    private float elapsedTime = 0f;

    public override void Initialize()
    {
        controller = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        // Reset stato
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        nextCheckpoint = 0;
        completedCheckpoints = 0;
        idleTimer = 0f;
        elapsedTime = 0f;

        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        lastDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = lastDist;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direzione al checkpoint
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(dir));

        // Velocità locale normalizzata
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.z / maxExpectedSpeed);
        sensor.AddObservation(localVel.x / (maxExpectedSpeed * 0.5f));

        // Progresso
        float progress = completedCheckpoints / (float)checkpointManager.TotalCheckpoints;
        sensor.AddObservation(progress);

        // Posizione rispetto all'avversario
        var oppAgent = opponent.GetComponent<BlueCarAgent>();
        bool isAhead = completedCheckpoints > (oppAgent != null ? oppAgent.GetCompletedCheckpoints() : 0);
        sensor.AddObservation(isAhead ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float accel = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steer = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);
        controller.Move(accel, steer, brake);

        elapsedTime += Time.fixedDeltaTime;

        // Penalità tempo
        AddReward(timePenalty * Time.fixedDeltaTime);

        // Reward di passo per step
        AddReward(-1f / maxStepsPerEpisode);

        // Progress smoothing reward
        float currentDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        smoothLastDist = smoothingAlpha * currentDist + (1f - smoothingAlpha) * smoothLastDist;
        AddReward((smoothLastDist - currentDist) * progressRewardMultiplier);
        lastDist = currentDist;

        // Reward velocità se guardo verso il checkpoint
        Vector3 toCP = (targetCheckpoint.position - transform.position).normalized;
        if (Vector3.Dot(transform.forward, toCP) > 0.5f)
        {
            AddReward((rb.linearVelocity.magnitude / maxExpectedSpeed) * speedRewardMultiplier * Time.fixedDeltaTime);
        }

        // Penalità di idle
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

        // Controllo checkpoint
        if (currentDist < 5f)
        {
            AddReward(checkpointReward / checkpointManager.TotalCheckpoints);
            completedCheckpoints++;
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;

            // Reward se supero avversario
            var oppAgent = opponent.GetComponent<BlueCarAgent>();
            if (oppAgent != null && completedCheckpoints > oppAgent.GetCompletedCheckpoints())
                AddReward(opponentBehindReward);
            else if (oppAgent != null && completedCheckpoints < oppAgent.GetCompletedCheckpoints())
                AddReward(opponentAheadPenalty);

            if (nextCheckpoint == 0)
            {
                // Lap completata
                AddReward(lapReward);
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
            EndEpisode(); // Fine episodio su collisione muro
        }
        else if (col.transform == opponent)
        {
            AddReward(opponentCollisionPenalty);
            Debug.Log("Red collided with Blue");
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;
}
