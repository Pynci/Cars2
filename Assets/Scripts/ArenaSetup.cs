using UnityEngine;

public class ArenaPairSetup : MonoBehaviour
{
    public Fase2RedCarAgent red;
    public Fase2BlueCarAgent blue;
    public RaceManager manager;

    void Awake()
    {
        red.opponent = blue.transform;
        blue.opponent = red.transform;

        red.raceManager = manager;
        blue.raceManager = manager;
    }
}
