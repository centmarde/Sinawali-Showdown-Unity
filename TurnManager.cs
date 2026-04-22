using System;
using UnityEngine;

/// <summary>
/// Minimal turn/phase state machine for a future turn-based game.
/// Kept separate from GameManager so you can evolve rules without bloating GameManager.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public enum TurnOwner
    {
        Player1,
        Player2
    }

    public enum TurnPhase
    {
        Start,
        Main,
        End
    }

    [Header("Turn State")]
    [SerializeField] private TurnOwner currentOwner = TurnOwner.Player1;
    [SerializeField] private TurnPhase currentPhase = TurnPhase.Start;
    [SerializeField] private int turnNumber = 1;

    public TurnOwner CurrentOwner => currentOwner;
    public TurnPhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;

    public event Action<TurnOwner, TurnPhase, int> OnTurnPhaseChanged;

    [ContextMenu("Next Phase")]
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.Start:
                SetPhase(currentOwner, TurnPhase.Main);
                break;
            case TurnPhase.Main:
                SetPhase(currentOwner, TurnPhase.End);
                break;
            case TurnPhase.End:
                AdvanceTurn();
                break;
            default:
                SetPhase(currentOwner, TurnPhase.Main);
                break;
        }
    }

    [ContextMenu("Reset Turn")]
    public void ResetTurn(TurnOwner startingOwner = TurnOwner.Player1)
    {
        turnNumber = 1;
        SetPhase(startingOwner, TurnPhase.Start);
    }

    public void SetPhase(TurnOwner owner, TurnPhase phase)
    {
        currentOwner = owner;
        currentPhase = phase;
        OnTurnPhaseChanged?.Invoke(currentOwner, currentPhase, turnNumber);
    }

    private void AdvanceTurn()
    {
        currentOwner = currentOwner == TurnOwner.Player1 ? TurnOwner.Player2 : TurnOwner.Player1;

        if (currentOwner == TurnOwner.Player1)
        {
            turnNumber++;
        }

        SetPhase(currentOwner, TurnPhase.Start);
    }
}
