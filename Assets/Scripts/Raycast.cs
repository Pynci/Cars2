using UnityEngine;

public class Raycast : MonoBehaviour
{
    public float rayLength;
    public int numberOfRays;
    public float angleSpan; // Gradi totali coperti dai ray

    public float[] rayDistances;
    [SerializeField] LayerMask layerMask;


    void Update()
    {
        rayDistances = new float[numberOfRays];
        float startAngle = -angleSpan / 2f;

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = startAngle + (angleSpan / (numberOfRays - 1)) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);
            RaycastHit hit;

            // Usa la maschera per ignorare il layer "Checkpoint"
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
        }
    }
}
