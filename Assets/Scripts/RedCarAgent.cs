using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]

public class RedCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;
    public CheckpointManager checkpointManager;
    public float maxSpeed = 30f;
    public float maxAngularSpeed = 180f;

    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;
    private float timeAtLastCheckpoint;
    private float lapStartTime;
    private int lapsCompleted;

    // Sistema di recupero collisioni
    private float lastCollisionTime;
    private int consecutiveCollisions;
    private Vector3 lastCollisionPoint;
    private const float MAX_RECOVERY_TIME = 3f;
    private const int MAX_CONSECUTIVE_COLLISIONS = 3;
    private const float RECOVERY_BONUS_DISTANCE = 3f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
        timeAtLastCheckpoint = Time.time;
        lapStartTime = Time.time;
        lapsCompleted = 0;
        consecutiveCollisions = 0;
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        //transform.position = rb.position;
        //transform.rotation = Quaternion.identity;

        int startIndex = 0;
        Vector3 spawnPos;

        // Oppure versione casuale:
        do
        {
            startIndex = Random.Range(0, checkpointManager.TotalCheckpoints);
            Transform startCp = checkpointManager.GetNextCheckpoint(startIndex);
            spawnPos = startCp.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            spawnPos.y = 0;
            Debug.Log(Physics.CheckSphere(spawnPos, 10f));
        } while (!Physics.CheckSphere(spawnPos, 10f));

        transform.position = spawnPos;

        nextCheckpointIndex = (startIndex + 1) % checkpointManager.TotalCheckpoints;
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);

        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        float baseAng = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, baseAng + Random.Range(-20f, 20f), 0f);

        nextCheckpointIndex = 0;
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, checkpointManager.GetNextCheckpoint(0).position);
        timeAtLastCheckpoint = Time.time;
        lapStartTime = Time.time;
        lapsCompleted = 0;
        consecutiveCollisions = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Velocità forward dell'agente (normalizzata)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        forwardSpeed = float.IsFinite(forwardSpeed) ? Mathf.Clamp(forwardSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(forwardSpeed / 20f);

        // Velocità laterale dell'agente
        float lateralSpeed = transform.InverseTransformDirection(rb.linearVelocity).x;
        lateralSpeed = float.IsFinite(lateralSpeed) ? Mathf.Clamp(lateralSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(lateralSpeed / 20f);


        // Distanza e direzione al prossimo checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDirection.x);
        sensor.AddObservation(localDirection.z);

        float checkpointDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        sensor.AddObservation(Mathf.Clamp(checkpointDistance / 100f, 0f, 1f));

        // Informazioni sull'avversario
        if (opponent != null)
        {
            Vector3 toOpponent = (opponent.position - transform.position).normalized;
            Vector3 localOpponentDir = transform.InverseTransformDirection(toOpponent);
            sensor.AddObservation(localOpponentDir.x);
            sensor.AddObservation(localOpponentDir.z);

            float opponentDistance = Vector3.Distance(transform.position, opponent.position);
            sensor.AddObservation(Mathf.Clamp(opponentDistance / 100f, 0f, 1f));
        }

        // Orientamento dell'auto (quaternion)
        sensor.AddObservation(transform.localRotation);

        // Stato di recupero
        //sensor.AddObservation(consecutiveCollisions > 0 ? 1f : 0f);
        //sensor.AddObservation(Mathf.Clamp((Time.time - lastCollisionTime) / MAX_RECOVERY_TIME, 0f, 1f));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Controllo tempo di recupero
        if (consecutiveCollisions > 0 && Time.time - lastCollisionTime > MAX_RECOVERY_TIME)
        {
            AddReward(-5f);
            EndEpisode();
            return;
        }

        // Estrai le azioni
        float accel = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        car.Move(accel, steer, brake);

        // Azioni di controllo
        float accelN = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float steerN = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float brakeN = Mathf.Clamp(actions.ContinuousActions[2], 0f, 1f);

        // Logica di recupero dopo collisione
        /*if (consecutiveCollisions > 0)
        {
            // Premia la retromarcia intelligente nei primi 2 secondi dopo collisione
            if (Time.time - lastCollisionTime < 2f)
            {
                if (accelN < -0.5f && brakeN > 0.5f)
                {
                    AddReward(0.2f); // Bonus per retromarcia
                }
                
                // Piccolo bonus per sterzata durante retromarcia
                if (Mathf.Abs(steerN) > 0.3f && accelN < -0.3f)
                {
                    AddReward(0.1f);
                }
            }
            
            // Premia il ritorno alla guida normale
            if (accelN > 0.5f && brakeN < 0.2f && Time.time - lastCollisionTime > 1f)
            {
                AddReward(0.1f);
            }
        }*/

        // Ricompensa per velocità (incoraggia movimento)
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed > 0.1f)
            AddReward(speed * 0.1f);
        else
            AddReward(-0.01f); // Piccola penalità per lentezza



        // Reward per progresso verso il checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float newDist = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDistanceToCheckpoint - newDist;

        // Modifica il reward in base allo stato di recupero
        float progressReward = delta * (consecutiveCollisions > 0 ? 0.1f : 0.2f);
        AddReward(progressReward);
        lastDistanceToCheckpoint = newDist;

        // Reset collision count se ci siamo allontanati dal punto di collisione
        if (consecutiveCollisions > 0 && newDist < RECOVERY_BONUS_DISTANCE)
        {
            AddReward(5f); // Grande bonus per il recupero
            consecutiveCollisions = 0;
        }

        // Reward per raggiungere un checkpoint
        if (newDist < 5f)
        {
            float timeToReach = Time.time - timeAtLastCheckpoint;
            float timeReward = Mathf.Clamp(2f / timeToReach, 0.5f, 5f);

            AddReward(10f + timeReward);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;
            timeAtLastCheckpoint = Time.time;

            // Reward per completamento giro
            if (nextCheckpointIndex == 0)
            {
                float lapTime = Time.time - lapStartTime;
                float lapReward = Mathf.Clamp(50f / lapTime, 10f, 100f);

                AddReward(lapReward);
                lapsCompleted++;
                lapStartTime = Time.time;

                if (lapsCompleted >= 3)
                {
                    EndEpisode();
                }
            }
        }

        // Reward per velocità allineata alla direzione del checkpoint
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        float velocityAlignment = Vector3.Dot(rb.linearVelocity.normalized, toTarget);
        float speedReward = Mathf.Clamp(localVelocity.z / maxSpeed, -0.5f, 1f) * velocityAlignment;
        AddReward(speedReward * 0.1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        c[1] = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f;
        c[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("bulkheads"))
        {
            float collisionPenalty = -2f * (1 + consecutiveCollisions * 0.5f);
            AddReward(collisionPenalty);

            lastCollisionTime = Time.time;
            lastCollisionPoint = transform.position;
            consecutiveCollisions++;

            if (consecutiveCollisions >= MAX_CONSECUTIVE_COLLISIONS)
            {
                AddReward(-5f);
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("BlueCar"))
        {
            AddReward(-5f);

            lastCollisionTime = Time.time;
            lastCollisionPoint = transform.position;
            consecutiveCollisions++;

            if (consecutiveCollisions >= MAX_CONSECUTIVE_COLLISIONS)
            {
                AddReward(-5f);
                EndEpisode();
            }
        }
    }
}