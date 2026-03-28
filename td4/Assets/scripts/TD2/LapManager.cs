using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LapManager : MonoBehaviour
{
    public List<Checkpoint> checkpoints;
    public int totalLaps = 3;
    public UIManager ui;

    private List<PlayerRank> playerRanks = new List<PlayerRank>();
    private PlayerRank mainPlayerRank;
    private int finishCheckpointIndex;
    public UnityEvent onPlayerFinished = new UnityEvent();

    IEnumerator Start()
    {
        // Wait for one frame to ensure all cars and tags are initialized
        yield return null;

        playerRanks.Clear();

        finishCheckpointIndex = ResolveFinishCheckpointIndex();

        foreach (CarIdentity carIdentity in FindObjectsByType<CarIdentity>(FindObjectsSortMode.None))
        {
            var playerRank = new PlayerRank(carIdentity)
            {
                lastCheckpoint = finishCheckpointIndex
            };

            playerRanks.Add(playerRank);
        }

        ListenCheckpoints(true);

        mainPlayerRank = playerRanks.Find(player => player.identity.gameObject.CompareTag("Player"));

        if (mainPlayerRank != null)
        {
            ui.UpdateLapText("Lap " + mainPlayerRank.lapNumber + " / " + totalLaps);
        }
        else
        {
            Debug.LogError("Still could not find Player tag! Check if the tag is on the ROOT of the car prefab.");
        }
    }

    private void ListenCheckpoints(bool subscribe)
    {
        if (checkpoints == null)
        {
            return;
        }

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            if (subscribe)
                checkpoint.onCheckpointEnter.AddListener(CheckpointActivated);
            else
                checkpoint.onCheckpointEnter.RemoveListener(CheckpointActivated);
        }
    }

    public void CheckpointActivated(CarIdentity car, Checkpoint checkpoint)
    {
        if (checkpoints == null || checkpoint == null)
        {
            return;
        }

        PlayerRank player = playerRanks.Find((rank) => rank.identity == car);
        if (checkpoints.Contains(checkpoint) && player!=null)
        {
            // if player has already finished don't do anything
            if (player.hasFinished) return;

            int checkpointNumber = checkpoints.IndexOf(checkpoint);
            if (checkpointNumber < 0 || checkpoints.Count == 0)
            {
                return;
            }

            int nextExpectedCheckpoint = (player.lastCheckpoint + 1) % checkpoints.Count;
            if (checkpointNumber != nextExpectedCheckpoint)
            {
                return;
            }

            player.lastCheckpoint = checkpointNumber;
            bool lapIsFinished = checkpointNumber == finishCheckpointIndex;

            if (lapIsFinished)
            {
                player.lapNumber += 1;
                player.lastCheckpoint = finishCheckpointIndex;

                // if this was the final lap
                if (player.lapNumber > totalLaps)
                {
                    player.hasFinished = true;
                    // getting final rank, by finding number of finished players
                    player.rank = playerRanks.FindAll(player => player.hasFinished).Count;

                    // if first winner, display its name
                    if (player.rank == 1)
                    {

                        // TODO : create attribute divername in CarIdentity 
                        //Debug.Log(player.identity.driverName + " won");
                        //ui.UpdateLapText(player.identity.driverName + " won");
                    }
                    else if (player == mainPlayerRank) // display player rank if not winner
                    {
                        ui.UpdateLapText("\nYou finished in " + mainPlayerRank.rank + " place");
                    }

                    if (player == mainPlayerRank)
                    {
                        Planet2WinScreenController winScreenController = FindFirstObjectByType<Planet2WinScreenController>();
                        if (winScreenController != null)
                        {
                            winScreenController.HandlePlayerFinished();
                        }
                        else
                        {
                            onPlayerFinished.Invoke();
                        }
                    }
                }
                else {
                    // TODO : create attribute divername in CarIdentity 
                    //Debug.Log(player.identity.driverName + ": lap " + player.lapNumber);
                    if (car.gameObject.tag == "Player") ui.UpdateLapText("Lap " + player.lapNumber + " / " + totalLaps);
                }
            }
        }
    }

    private int ResolveFinishCheckpointIndex()
    {
        if (checkpoints == null || checkpoints.Count == 0)
        {
            return 0;
        }

        for (int i = 0; i < checkpoints.Count; i++)
        {
            Checkpoint checkpoint = checkpoints[i];
            if (checkpoint != null && checkpoint.gameObject.name.Contains("StartFinish"))
            {
                return i;
            }
        }

        return 0;
    }
}
