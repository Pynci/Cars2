using UnityEngine;
using Unity.MLAgents;

public class RaceManager : MonoBehaviour
{
    public Fase2RedCarAgent redAgent;
    public Fase2BlueCarAgent blueAgent;

    private bool raceOver = false;

    public void NotifyLapCompleted(Agent winner)
    {
        if (raceOver) return;
        raceOver = true;

        // Penalit� al perdente (solo se non ha completato il giro)
        if (winner == redAgent && !blueAgent.HasCompletedLap())
        {
            float progress = blueAgent.GetProgress();
            float penalty = -20f * (1f - progress);
            blueAgent.AddReward(penalty);
            blueAgent.EndEpisode();
        }
        else if (winner == blueAgent && !redAgent.HasCompletedLap())
        {
            float progress = redAgent.GetProgress();
            float penalty = -20f * (1f - progress);
            redAgent.AddReward(penalty);
            redAgent.EndEpisode();
        }

        // Termina anche per il vincitore
        winner.EndEpisode();
        raceOver = false;
    }
}
