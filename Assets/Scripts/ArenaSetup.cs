using UnityEngine;

public class ArenaSetup : MonoBehaviour
{
    public Fase1BlueCarAgent blueF1;
    public Fase1RedCarAgent redF1;
    public Fase1RoseCarAgent roseF1;
    public Fase1GreenCarAgent greenF1;
    public Fase1YellowCarAgent yellowF1;
    public Fase1VioletCarAgent violetF1;

    public Fase2BlueCarAgent blueF2;
    public Fase2RedCarAgent redF2;
    public Fase2RoseCarAgent roseF2;
    public Fase2GreenCarAgent greenF2;
    public Fase2YellowCarAgent yellowF2;
    public Fase2VioletCarAgent violetF2;

    public RaceManager manager;

    void Awake()
    {
        // Get all transforms
        Transform[] allAgents = new Transform[]
        {
            blueF1.transform,
            redF1.transform,
            roseF1.transform,
            greenF1.transform,
            yellowF1.transform,
            violetF1.transform
        };

        // Assign opponents (all others)
        AssignOpponents(blueF1, allAgents);
        AssignOpponents(redF1, allAgents);
        AssignOpponents(roseF1, allAgents);
        AssignOpponents(greenF1, allAgents);
        AssignOpponents(yellowF1, allAgents);
        AssignOpponents(violetF1, allAgents);

        // Assign RaceManager only to Fase2 agents
        blueF2.raceManager = manager;
        redF2.raceManager = manager;
        roseF2.raceManager = manager;
        greenF2.raceManager = manager;
        yellowF2.raceManager = manager;
        violetF2.raceManager = manager;
    }

    void AssignOpponents(MonoBehaviour agent, Transform[] allAgents)
    {
        // This assumes your agent scripts have a public Transform[] opponents field
        Transform self = ((Component)agent).transform;

        int count = 0;
        Transform[] opponents = new Transform[allAgents.Length - 1];
        foreach (var t in allAgents)
        {
            if (t != self)
                opponents[count++] = t;
        }

        // Try to set the 'opponents' field if present
        var field = agent.GetType().GetField("opponents");
        if (field != null)
        {
            field.SetValue(agent, opponents);
        }
    }
}
