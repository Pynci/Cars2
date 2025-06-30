using UnityEngine;

public class ArenaPairSetup : MonoBehaviour
{
    public RedCarAgent red;
    public BlueCarAgent blue;

    void Awake()
    {
        red.opponent = blue.transform;
        blue.opponent = red.transform;
    }
}
