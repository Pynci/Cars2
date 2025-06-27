using UnityEngine;

public class Raycast : MonoBehaviour
{
    public float rayLength;
    public int numberOfRays;
    public float angleSpan; 

    public float[] rayDistances;
    public float[] rayAngles; // Array degli angoli relativi ai raggi

    [SerializeField] LayerMask layerMask;

    public float BestRayAngle { get; private set; } = 0f; // Angolo del ray più libero

    void Start()
    {
        rayDistances = new float[numberOfRays];
        rayAngles = new float[numberOfRays];
    }

    void Update()
    {
        float startAngle = -angleSpan / 2f;
        float maxDistance = -1f;
        int bestIndex = 0;

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = startAngle + (angleSpan / (numberOfRays - 1)) * i;
            rayAngles[i] = angle;

            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayLength, ~layerMask))
            {
                rayDistances[i] = hit.distance;
                Debug.DrawRay(ray.origin, direction * hit.distance, Color.red);
            }
            else
            {
                rayDistances[i] = rayLength;
                Debug.DrawRay(ray.origin, direction * rayLength, Color.green);
            }

            if (rayDistances[i] > maxDistance)
            {
                maxDistance = rayDistances[i];
                bestIndex = i;
            }
        }

        BestRayAngle = rayAngles[bestIndex]; // salva l’angolo migliore per l’agente
    }
}

