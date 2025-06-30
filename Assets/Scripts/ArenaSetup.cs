using UnityEngine;

public class ArenaPairSetup : MonoBehaviour
{
    public RedCarAgent red;
    public BlueCarAgent blue;
    public RaceManager manager;

    void Awake()
    {
        red.opponent = blue.transform;
        blue.opponent = red.transform;

        red.raceManager = manager;
        blue.raceManager = manager;
    }
}
