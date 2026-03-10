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
    public UnityEvent onPlayerFinished = new UnityEvent();

    IEnumerator Start()
    {
        // Wait for one frame to ensure all cars and tags are initialized
        yield return null;

        foreach (CarIdentity carIdentity in GameObject.FindObjectsOfType<CarIdentity>())
        {
            playerRanks.Add(new PlayerRank(carIdentity));
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
        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (subscribe)
                checkpoint.onCheckpointEnter.AddListener(CheckpointActivated);
            else
                checkpoint.onCheckpointEnter.RemoveListener(CheckpointActivated);
        }
    }

    public void CheckpointActivated(CarIdentity car, Checkpoint checkpoint)
    {
        PlayerRank player = playerRanks.Find((rank) => rank.identity == car);
        if (checkpoints.Contains(checkpoint) && player!=null)
        {
            // if player has already finished don't do anything
            if (player.hasFinished) return;

            int checkpointNumber = checkpoints.IndexOf(checkpoint);
            // first time ever the car reach the first checkpoint
            bool startingFirstLap = checkpointNumber == 0 && player.lastCheckpoint == -1;
            // finish line checkpoint is triggered & last checkpoint was reached
            bool lapIsFinished = checkpointNumber == 0 && player.lastCheckpoint >= checkpoints.Count - 1;
            if (startingFirstLap || lapIsFinished) 
            { 
                player.lapNumber += 1;
                player.lastCheckpoint = 0;

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

                    if (player == mainPlayerRank) onPlayerFinished.Invoke();
                }
                else {
                    // TODO : create attribute divername in CarIdentity 
                    //Debug.Log(player.identity.driverName + ": lap " + player.lapNumber);
                    if (car.gameObject.tag == "Player") ui.UpdateLapText("Lap " + player.lapNumber + " / " + totalLaps);
                }
            }
            // next checkpoint reached
            else if (checkpointNumber == player.lastCheckpoint + 1)
            {
                player.lastCheckpoint += 1;
            }
        }
    }
}
