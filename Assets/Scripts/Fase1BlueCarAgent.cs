
using System.Drawing;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class Fase1BlueCarAgent : Agent
{
    [Header("Setup")]
    public CheckpointManager checkpointManager;
    public Transform opponent;

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

        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        float baseAng = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, baseAng + Random.Range(-20f, 20f), 0f);
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
        var oppAgent = opponent.GetComponent<Fase1RedCarAgent>();
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
        AddReward(0.005f);

        // Ricompensa per avvicinamento al checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpoint);
        float newDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float progressDelta = lastDist - newDistance;
        if (progressDelta > 0)
            AddReward(progressDelta * 0.2f);  // Aumentato rispetto al tuo (0.1f → 0.2f)
        lastDist = newDistance;

        // Ricompensa per raggiungimento checkpoint
        if (newDistance < 5f)
        {
            AddReward(10f);
            nextCheckpoint = (nextCheckpoint + 1) % checkpointManager.TotalCheckpoints;
        }

        // Ricompensa per velocità solo se si muove verso checkpoint
        Vector3 dirToCheckpoint = (targetCheckpoint.position - transform.position).normalized;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity.normalized, dirToCheckpoint);
        if (forwardSpeed > 0.5f)
            AddReward(forwardSpeed*0.05f);  // Solo se va "realmente avanti"
        else
            AddReward(-0.02f);  // Penalità soft se si muove male o va indietro
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
            AddReward(-10f);
            EndEpisode(); // solo questa macchina
        }
        else if (col.transform == opponent)
        {
            AddReward(-10f);
            EndEpisode();
        }
    }

    public int GetCompletedCheckpoints() => completedCheckpoints;

}