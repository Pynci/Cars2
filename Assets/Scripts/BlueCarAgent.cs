using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class BlueCarAgent : Agent
{
    [Header("Agent Configuration")]
    public Transform opponent;
    public CheckpointManager checkpointManager;
    public Transform[] spawnPoints;

    [Header("Simple Reward Settings")]
    public float checkpointReward = 15f;
    public float lapCompleteReward = 100f;
    public float collisionPenalty = -20f;

    private Rigidbody rb;
    private CarController car;
    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;
    private Vector3 lastPosition;
    private float stuckTimer;
    private float episodeTimer;
    private int checkpointsHit;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();

        // Configurazione rigidbody
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.mass = 1200f;
    }

    public override void OnEpisodeBegin()
    {
        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset variables
        nextCheckpointIndex = 0;
        checkpointsHit = 0;
        stuckTimer = 0f;
        episodeTimer = 0f;

        // Spawn position
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[spawnIndex].position;
            transform.rotation = spawnPoints[spawnIndex].rotation;
        }
        else
        {
            transform.position = new Vector3(-207.7f, 0.5f, 53f);
            transform.rotation = Quaternion.identity;
        }

        // Variazione casuale
        transform.position += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        transform.rotation *= Quaternion.Euler(0, Random.Range(-10f, 10f), 0);

        // Setup checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        lastPosition = transform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === OSSERVAZIONI SEMPLICI ===

        // Velocità locale
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(localVelocity.z, -25f, 25f) / 25f); // Forward speed
        sensor.AddObservation(Mathf.Clamp(localVelocity.x, -10f, 10f) / 10f);  // Lateral speed

        // Direzione verso checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDirection.x);
        sensor.AddObservation(localDirection.z);

        // Distanza checkpoint
        float distance = Vector3.Distance(transform.position, targetCheckpoint.position);
        sensor.AddObservation(Mathf.Clamp(distance, 0f, 80f) / 80f);

        // Angolo verso target
        float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up) / 180f;
        sensor.AddObservation(angle);

        // Orientamento
        sensor.AddObservation(transform.localRotation);

        // Progresso semplice
        sensor.AddObservation((float)checkpointsHit / checkpointManager.TotalCheckpoints);

        // Opponent (opzionale)
        if (opponent != null)
        {
            Vector3 toOpponent = opponent.position - transform.position;
            sensor.AddObservation(Mathf.Clamp(toOpponent.magnitude, 0f, 40f) / 40f);
        }
        else
        {
            sensor.AddObservation(1f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Azioni
        float motor = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steering = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);

        car.Move(motor, steering, brake);
        episodeTimer += Time.fixedDeltaTime;

        // === SISTEMA DI REWARD SEMPLICE ===

        // 1. Reward principale: progresso verso checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float currentDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float progress = lastDistanceToCheckpoint - currentDistance;
        AddReward(progress * 2f); // Reward più grande per semplicità
        lastDistanceToCheckpoint = currentDistance;

        // 2. Reward per velocità (semplice)
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (speed > 0.5f)
        {
            AddReward(0.1f * Time.fixedDeltaTime);
        }

        // 3. Check checkpoint
        if (currentDistance < 10f) // Raggio più permissivo
        {
            AddReward(checkpointReward);
            checkpointsHit++;
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;

            Debug.Log($"BlueCar: Checkpoint {checkpointsHit}/{checkpointManager.TotalCheckpoints}");

            if (nextCheckpointIndex == 0)
            {
                AddReward(lapCompleteReward);
                Debug.Log("BlueCar: Giro completato!");
                EndEpisode();
                return;
            }

            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
            lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        }

        // 4. Check stuck (semplice)
        float movement = Vector3.Distance(transform.position, lastPosition);
        if (movement < 0.1f && speed < 0.3f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 4f)
            {
                AddReward(-3f);
                Debug.Log("BlueCar: Stuck - Restart");
                EndEpisode();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // 5. Timeout
        if (episodeTimer > 150f) // Timeout più permissivo
        {
            AddReward(-5f);
            Debug.Log("BlueCar: Timeout");
            EndEpisode();
            return;
        }

        // 6. Survival reward
        AddReward(0.01f);

        lastPosition = transform.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Vertical");
        actions[1] = Input.GetAxis("Horizontal");
        actions[2] = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            Debug.Log("BlueCar: Wall collision - Restart");
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("BlueCar") || collision.gameObject.CompareTag("RedCar"))
        {
            AddReward(-5f);
            Debug.Log("BlueCar: Car collision");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CheckPoint"))
        {
            int checkpointIndex = System.Array.IndexOf(checkpointManager.checkpoints, other.transform);
            if (checkpointIndex == nextCheckpointIndex)
            {
                AddReward(checkpointReward);
                checkpointsHit++;
                nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;

                if (nextCheckpointIndex == 0)
                {
                    AddReward(lapCompleteReward);
                    EndEpisode();
                }
            }
        }
    }
}