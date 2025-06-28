using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BlueCarAgent : Agent
{
    Rigidbody rb;
    CarController car;
    public Transform opponent;
    public CheckpointManager checkpointManager;

    // Rimuovi questi riferimenti al raycast personalizzato
    // public Raycast raycast;
    // public RayPerceptionSensorComponent3D rayCast; // Non serve un riferimento esplicito

    private int nextCheckpointIndex;
    private Transform targetCheckpoint;
    private float lastDistanceToCheckpoint;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        car = GetComponent<CarController>();
        // Rimuovi: rayLength = Mathf.Max(raycast.rayLength, 0.01f);
    }

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Spawn casuale come nel codice precedente o fisso come preferisci
        // Versione fissa:
        transform.position = new Vector3(-207.7f, 0f, 53f);
        transform.rotation = Quaternion.identity;

        // Oppure versione casuale:
        /*
        int startIndex = Random.Range(0, checkpointManager.TotalCheckpoints);
        Transform startCp = checkpointManager.GetNextCheckpoint(startIndex);
        Vector3 spawnPos = startCp.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        spawnPos.y = startCp.position.y;
        transform.position = spawnPos;
        
        nextCheckpointIndex = (startIndex + 1) % checkpointManager.TotalCheckpoints;
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
        
        Vector3 dir = (targetCheckpoint.position - transform.position).normalized;
        float baseAng = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, baseAng + Random.Range(-20f, 20f), 0f);
        */

        // Inizializza il checkpoint system
        nextCheckpointIndex = 0;
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        lastDistanceToCheckpoint = Vector3.Distance(transform.position, targetCheckpoint.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // RIMUOVI tutto il codice relativo ai raggi personalizzati
        // ML-Agents gestisce automaticamente RayPerceptionSensorComponent3D

        // Mantieni solo le osservazioni personalizzate:

        // Velocità forward dell'agente (normalizzata)
        float forwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        forwardSpeed = float.IsFinite(forwardSpeed) ? Mathf.Clamp(forwardSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(forwardSpeed / 20f);

        // Velocità laterale dell'agente
        float lateralSpeed = transform.InverseTransformDirection(rb.linearVelocity).x;
        lateralSpeed = float.IsFinite(lateralSpeed) ? Mathf.Clamp(lateralSpeed, -20f, 20f) : 0f;
        sensor.AddObservation(lateralSpeed / 20f);

        // Distanza dall'opponent (normalizzata)
        float opponentDistance = Vector3.Distance(transform.position, opponent.position);
        opponentDistance = float.IsFinite(opponentDistance) ? Mathf.Clamp(opponentDistance, 0f, 100f) : 100f;
        sensor.AddObservation(opponentDistance / 100f);

        // Rotazione Y normalizzata
        float yaw = (transform.eulerAngles.y % 360f + 360f) % 360f;
        sensor.AddObservation(yaw / 360f);

        // Distanza dal checkpoint target (normalizzata)
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float checkpointDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        checkpointDistance = float.IsFinite(checkpointDistance) ? Mathf.Clamp(checkpointDistance, 0f, 100f) : 100f;
        sensor.AddObservation(checkpointDistance / 100f);

        // Direzione verso il checkpoint target (in coordinate locali)
        Vector3 toTarget = (targetCheckpoint.position - transform.position).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(toTarget);
        sensor.AddObservation(localDirection.x);
        sensor.AddObservation(localDirection.z);

        // Rotazione dell'agente (quaternion)
        sensor.AddObservation(transform.localRotation);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Estrai le azioni
        float accel = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        float brake = actions.ContinuousActions[2];

        // Controllo validità
        if (!float.IsFinite(accel) || !float.IsFinite(steer) || !float.IsFinite(brake)) return;

        // Applica le azioni al controller
        car.Move(accel, steer, brake);

        // Sistema di ricompense

        // Ricompensa base per rimanere attivo
        AddReward(0.01f);

        // Ricompensa per avvicinamento al checkpoint
        targetCheckpoint = checkpointManager.GetNextCheckpoint(nextCheckpointIndex);
        float newDistance = Vector3.Distance(transform.position, targetCheckpoint.position);
        float delta = lastDistanceToCheckpoint - newDistance;
        AddReward(delta * 0.1f);
        lastDistanceToCheckpoint = newDistance;

        // Ricompensa per raggiungimento checkpoint
        if (newDistance < 5f)
        {
            AddReward(5f);
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;

            if (nextCheckpointIndex == 0)
            {
                // Completato un giro completo
                AddReward(10f);
                EndEpisode();
            }
        }

        // Ricompensa per velocità (incoraggia movimento)
        float speed = transform.InverseTransformDirection(rb.linearVelocity).z;
        if (speed > 0.1f)
            AddReward(speed * 0.02f);
        else
            AddReward(-0.01f); // Piccola penalità per lentezza

        // RIMUOVI: Le penalità per vicinanza ostacoli ora sono gestite automaticamente
        // da RayPerceptionSensorComponent3D tramite il sistema di ricompense di ML-Agents
        // o puoi gestirle tramite Curiosity/altri moduli reward
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.LeftArrow) ? -1f : Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bulkheads"))
        {
            AddReward(-10f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("BlueCar") || collision.gameObject.CompareTag("RedCar"))
        {
            AddReward(-5f);
        }
    }
}