using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// The end battle state is really just a placeholder for now which causes the scene to reload.
// It could be replaced by a sequence of several other states,
// such as showing what items you had earned and how much experience you had gained.
// Eventually it would be responsible for returning to some other part of the game where you can manage your team, choose other missions, etc.
public class EndBattleState : BattleState
{
    public override void Enter()
    {
        base.Enter();
        SceneManager.LoadScene(0);
    }
}