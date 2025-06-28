using UnityEngine;

public class Raycast : MonoBehaviour
{
    public float rayLength;
    public int numberOfRays;
    public float angleSpan;

    public float[] rayDistances;
    public float[] rayAngles;

    [SerializeField] LayerMask layerMask;
    [SerializeField] Transform rayOrigin;

    public float BestRayAngle { get; private set; } = 0f;

    void Start()
    {
        rayDistances = new float[numberOfRays];
        rayAngles = new float[numberOfRays];
    }

    void FixedUpdate()
    {
        float startAngle = -angleSpan / 2f;
        float maxDistance = -1f;
        int bestIndex = 0;

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = startAngle + (angleSpan / (numberOfRays - 1)) * i;
            rayAngles[i] = angle;

            Vector3 direction = Quaternion.Euler(0, angle, 0) * rayOrigin.forward;
            Vector3 origin = rayOrigin.position;

            Ray ray = new Ray(origin, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayLength, ~layerMask))
            {
                rayDistances[i] = hit.distance;
                Debug.DrawRay(origin, direction * hit.distance, Color.red);
            }
            else
            {
                rayDistances[i] = rayLength;
                Debug.DrawRay(origin, direction * rayLength, Color.green);
            }

            if (rayDistances[i] > maxDistance)
            {
                maxDistance = rayDistances[i];
                bestIndex = i;
            }
        }

        BestRayAngle = rayAngles[bestIndex];
    }
}