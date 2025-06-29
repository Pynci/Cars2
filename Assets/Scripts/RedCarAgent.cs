using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class RedCarAgent : Agent
{
    [Header("Agent Configuration")]
    public Transform opponent;
    public CheckpointManager checkpointManager;
    public Transform[] spawnPoints;

    [Header("Reward Tuning")]
    public float checkpointReward = 10f;
    public float lapCompleteReward = 50f;
    public float collisionPenalty = -15f;
    public float speedRewardMultiplier = 0.1f;
    public float progressRewardMultiplier = 1f;

    private Rigidbody rb;
    private CarController car;
    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;
    private Vector3 lastPosition;
    private float stuckTimer;
    private float episodeTimer;
    private int checkpointsHit;

    // Performance tracking
    private float bestLapTime = float.MaxValue;
    private float currentLapStartTime;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();

        // Assicurati che il rigidbody abbia le giuste impostazioni
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Abbassa il centro di massa
        rb.mass = 1200f; // Massa realistica per un'auto
    }

    public override void OnEpisodeBegin()
    {
        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset tracking variables
        nextCheckpointIndex = 0;
        checkpointsHit = 0;
        stuckTimer = 0f;
        episodeTimer = 0f;
        currentLapStartTime = Time.time;

        // Spawn randomization per migliorare la generalizzazione
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[spawnIndex].position;
            transform.rotation = spawnPoints[spawnIndex].rotation;
        }
        else
        {
            // Fallback position
            transform.position = new Vector3(-213.1f, 0.5f, 53f);
            transform.rotation = Quaternion.identity;
        }

        // Aggiungi piccola variazione casuale per evitare comportamenti identici
        transform.position += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        transform.rotation *= Quaternion.Euler(0, Random.Range(-10f, 10f), 0);

        // Inizializza checkpoint system
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        lastPosition = transform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === OSSERVAZIONI MOVIMENTO ===
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        // Velocità forward/backward (normalizzata)
        float forwardSpeed = Mathf.Clamp(localVelocity.z, -30f, 30f) / 30f;
        sensor.AddObservation(forwardSpeed);

        // Velocità laterale (drift)
        float lateralSpeed = Mathf.Clamp(localVelocity.x, -15f, 15f) / 15f;
        sensor.AddObservation(lateralSpeed);

        // Velocità angolare Y (rotazione)
        float angularVelY = Mathf.Clamp(rb.angularVelocity.y, -5f, 5f) / 5f;
        sensor.AddObservation(angularVelY);

        // === OSSERVAZIONI CHECKPOINT ===
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);

        // Distanza dal checkpoint target
        float checkpointDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        sensor.AddObservation(Mathf.Clamp(checkpointDistance, 0f, 100f) / 100f);

        // Direzione verso il checkpoint (coordinate locali)
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDirection.x); // Quanto è a sinistra/destra
        sensor.AddObservation(localDirection.z); // Quanto è avanti/dietro

        // Angolo tra direzione auto e direzione checkpoint
        float angleToTarget = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up) / 180f;
        sensor.AddObservation(angleToTarget);

        // === OSSERVAZIONI AVVERSARIO ===
        if (opponent != null)
        {
            Vector3 toOpponent = opponent.position - transform.position;
            float opponentDistance = toOpponent.magnitude;
            sensor.AddObservation(Mathf.Clamp(opponentDistance, 0f, 50f) / 50f);

            // Direzione avversario (coordinate locali)
            Vector3 localOpponentDir = transform.InverseTransformDirection(toOpponent.normalized);
            sensor.AddObservation(localOpponentDir.x);
            sensor.AddObservation(localOpponentDir.z);
        }
        else
        {
            // Padding se non c'è avversario
            sensor.AddObservation(1f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // === OSSERVAZIONI ORIENTAMENTO ===
        // Rotazione corrente (quaternion normalizzato)
        sensor.AddObservation(transform.localRotation);

        // === OSSERVAZIONI CONTESTO ===
        // Progress nel giro (quanti checkpoint completati)
        sensor.AddObservation((float)checkpointsHit / checkpointManager.TotalCheckpoints);

        // Timer episodio normalizzato
        sensor.AddObservation(Mathf.Clamp01(episodeTimer / 120f)); // Max 2 minuti per episodio
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Estrai azioni
        float motor = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steering = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brake = Mathf.Clamp01(actions.ContinuousActions[2]);

        // Applica azioni
        car.Move(motor, steering, brake);

        // Update timers
        episodeTimer += Time.fixedDeltaTime;

        // === SISTEMA DI REWARD ARTICOLATO ===

        // 1. Reward per progresso verso checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float currentDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float progressReward = (lastDistanceToCheckpoint - currentDistance) * progressRewardMultiplier;
        AddReward(progressReward);
        lastDistanceToCheckpoint = currentDistance;

        // 2. Reward per velocità (incoraggia movimento)
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (speed > 1f)
        {
            AddReward(speed * speedRewardMultiplier * Time.fixedDeltaTime);
        }

        // 3. Reward per direzione corretta
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        float directionAlignment = Vector3.Dot(transform.forward, toTarget);
        AddReward(directionAlignment * 0.02f * Time.fixedDeltaTime);

        // 4. Penalità per deriva eccessiva
        float driftPenalty = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.right)) * 0.01f;
        AddReward(-driftPenalty * Time.fixedDeltaTime);

        // 5. Check checkpoint raggiunto
        if (currentDistance < 8f) // Aumentato il raggio per essere più permissivi
        {
            AddReward(checkpointReward);
            checkpointsHit++;
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;

            Debug.Log($"RedCar: Checkpoint {checkpointsHit}/{checkpointManager.TotalCheckpoints} raggiunto!");

            // Check giro completato
            if (nextCheckpointIndex == 0)
            {
                float lapTime = Time.time - currentLapStartTime;
                AddReward(lapCompleteReward);

                if (lapTime < bestLapTime)
                {
                    bestLapTime = lapTime;
                    AddReward(20f); // Bonus per record personale
                }

                Debug.Log($"RedCar: Giro completato in {lapTime:F2}s!");
                EndEpisode();
                return;
            }

            // Update target
            targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
            lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        }

        // 6. Check se l'agente è bloccato
        float movementThisFrame = Vector3.Distance(transform.position, lastPosition);
        if (movementThisFrame < 0.1f && speed < 0.5f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 3f)
            {
                AddReward(-5f);
                Debug.Log("RedCar: Riavvio per blocco");
                EndEpisode();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // 7. Timeout episodio
        if (episodeTimer > 120f) // 2 minuti max
        {
            AddReward(-10f);
            Debug.Log("RedCar: Timeout episodio");
            EndEpisode();
            return;
        }

        // 8. Piccolo reward per sopravvivenza
        AddReward(0.001f);

        lastPosition = transform.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Vertical");   // W/S
        actions[1] = Input.GetAxis("Horizontal"); // A/D
        actions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f; // Space per freno
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bulkheads"))
        {
            AddReward(collisionPenalty);
            Debug.Log("RedCar: Collisione con muro - Riavvio");
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("BlueCar") || collision.gameObject.CompareTag("RedCar"))
        {
            AddReward(-8f); // Penalità più leggera per collisioni tra auto
            Debug.Log("RedCar: Collisione con altra auto");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Gestione alternativa per checkpoint se sono trigger
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