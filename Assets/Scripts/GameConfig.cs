using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public static GameConfig Main { get; private set; }

    public bool DebugComputerPlayer;
    public bool DebugPathfinding;
    public bool MakeAllUnitsSeeEachOther; // Only works at the start

    private void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(this);
        }
        else
        {
            Main = this;
        }
    }
}
