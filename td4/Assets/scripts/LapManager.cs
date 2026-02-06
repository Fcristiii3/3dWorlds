using System.Collections.Generic;
using UnityEngine;

public class LapManager : MonoBehaviour
{
    public List<SimpleCheckpoint> checkpoints;
    public int totalLaps = 3;

    // 1. ADD THE REFERENCE TO THE UI MANAGER
    public UIManager uiManager;

    private int lastPlayerCheckpoint = -1;
    private int currentPlayerLap = 0;

    void Start()
    {
        ListenCheckpoints(true);

        // 2. INITIALIZE THE UI ON START
        if (uiManager != null)
            uiManager.UpdateLapText("Lap: " + currentPlayerLap + "/" + totalLaps);
    }

    private void ListenCheckpoints(bool subscribe)
    {
        foreach (SimpleCheckpoint checkpoint in checkpoints)
        {
            if (subscribe) checkpoint.onCheckpointEnter.AddListener(CheckpointActivated);
            else checkpoint.onCheckpointEnter.RemoveListener(CheckpointActivated);
        }
    }

    public void CheckpointActivated(GameObject car, SimpleCheckpoint checkpoint)
    {
        if (checkpoints.Contains(checkpoint))
        {
            int checkpointNumber = checkpoints.IndexOf(checkpoint);
            bool startingFirstLap = checkpointNumber == 0 && lastPlayerCheckpoint == -1;
            bool lapIsFinished = checkpointNumber == 0 && lastPlayerCheckpoint >= checkpoints.Count - 1;

            if (startingFirstLap || lapIsFinished)
            {
                currentPlayerLap += 1;
                lastPlayerCheckpoint = 0;

                if (currentPlayerLap > totalLaps)
                {
                    Debug.Log("You won");
                    // 3. UPDATE UI FOR WIN STATE
                    if (uiManager != null) uiManager.UpdateLapText("Race Finished!");
                }
                else
                {
                    Debug.Log("Lap " + currentPlayerLap);
                    // 4. UPDATE UI FOR NEW LAP
                    if (uiManager != null)
                        uiManager.UpdateLapText("Lap: " + currentPlayerLap + "/" + totalLaps);
                }
            }
            else if (checkpointNumber == lastPlayerCheckpoint + 1)
            {
                lastPlayerCheckpoint += 1;
            }
        }
    }
}