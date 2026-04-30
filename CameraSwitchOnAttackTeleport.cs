using UnityEngine;

/// <summary>
/// Attach to the camera (or a camera rig). When an attack-teleport sequence starts,
/// this script snaps the camera to a transform from an inspector array.
/// It can optionally restore the original camera transform when the sequence ends.
/// </summary>
public class CameraSwitchOnAttackTeleport : MonoBehaviour
{
    [Header("Listen To (Optional)")]
    [Tooltip("If assigned, listens to Player1 teleport+attack events from this component.")]
    [SerializeField] private AttackTeleportOnAttackCardConfirm player1AttackTeleport;

    [Tooltip("If assigned, listens to Player2 teleport+attack events from this component.")]
    [SerializeField] private Player2AttackTeleportOnAITurn player2AttackTeleport;

    [Header("Camera Points")]
    [Tooltip("Camera will snap to 1 random transform from this list when Player1's teleport-attack starts.")]
    [SerializeField] private Transform[] player1CameraPoints;

    [Tooltip("Camera will snap to 1 random transform from this list when Player2's teleport-attack starts.")]
    [SerializeField] private Transform[] player2CameraPoints;

    [Header("Restore")]
    [Tooltip("If true, restores this transform after the teleport-attack sequence ends.")]
    [SerializeField] private bool restoreAfterAttack = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private bool hasOriginalPose;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void OnEnable()
    {
        AutoResolveReferencesIfNeeded();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void AutoResolveReferencesIfNeeded()
    {
        if (player1AttackTeleport == null)
        {
            player1AttackTeleport = FindObjectOfType<AttackTeleportOnAttackCardConfirm>();
        }

        if (player2AttackTeleport == null)
        {
            player2AttackTeleport = FindObjectOfType<Player2AttackTeleportOnAITurn>();
        }
    }

    private void Subscribe()
    {
        if (player1AttackTeleport != null)
        {
            player1AttackTeleport.OnTeleportAttackStarted -= HandlePlayer1TeleportAttackStarted;
            player1AttackTeleport.OnTeleportAttackStarted += HandlePlayer1TeleportAttackStarted;
            player1AttackTeleport.OnTeleportAttackEnded -= HandleTeleportAttackEnded;
            player1AttackTeleport.OnTeleportAttackEnded += HandleTeleportAttackEnded;
        }

        if (player2AttackTeleport != null)
        {
            player2AttackTeleport.OnTeleportAttackStarted -= HandlePlayer2TeleportAttackStarted;
            player2AttackTeleport.OnTeleportAttackStarted += HandlePlayer2TeleportAttackStarted;
            player2AttackTeleport.OnTeleportAttackEnded -= HandleTeleportAttackEnded;
            player2AttackTeleport.OnTeleportAttackEnded += HandleTeleportAttackEnded;
        }
    }

    private void Unsubscribe()
    {
        if (player1AttackTeleport != null)
        {
            player1AttackTeleport.OnTeleportAttackStarted -= HandlePlayer1TeleportAttackStarted;
            player1AttackTeleport.OnTeleportAttackEnded -= HandleTeleportAttackEnded;
        }

        if (player2AttackTeleport != null)
        {
            player2AttackTeleport.OnTeleportAttackStarted -= HandlePlayer2TeleportAttackStarted;
            player2AttackTeleport.OnTeleportAttackEnded -= HandleTeleportAttackEnded;
        }
    }

    private void HandlePlayer1TeleportAttackStarted(Transform actor)
    {
        SwitchToRandomPoint(player1CameraPoints, "Player1", actor);
    }

    private void HandlePlayer2TeleportAttackStarted(Transform actor)
    {
        SwitchToRandomPoint(player2CameraPoints, "Player2", actor);
    }

    private void SwitchToRandomPoint(Transform[] points, string label, Transform actor)
    {
        if (points == null || points.Length == 0)
        {
            if (logDebug)
            {
                Debug.LogWarning($"CameraSwitchOnAttackTeleport: No camera points assigned for {label}.", this);
            }
            return;
        }

        if (!hasOriginalPose)
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            hasOriginalPose = true;
        }

        Transform point = GetRandomNonNullPoint(points, out int index);
        if (point == null)
        {
            if (logDebug)
            {
                Debug.LogWarning($"CameraSwitchOnAttackTeleport: All camera points are null for {label}.", this);
            }
            return;
        }

        transform.SetPositionAndRotation(point.position, point.rotation);

        if (logDebug)
        {
            Debug.Log($"CameraSwitchOnAttackTeleport: Switched camera to {label} point {index} for actor '{(actor != null ? actor.name : "(null)")}'.", this);
        }
    }

    private static Transform GetRandomNonNullPoint(Transform[] points, out int chosenIndex)
    {
        chosenIndex = -1;

        if (points == null || points.Length == 0)
        {
            return null;
        }

        // Try a few random picks first.
        int attempts = Mathf.Min(8, points.Length);
        for (int i = 0; i < attempts; i++)
        {
            int idx = Random.Range(0, points.Length);
            Transform t = points[idx];
            if (t != null)
            {
                chosenIndex = idx;
                return t;
            }
        }

        // Fallback: deterministic scan for first non-null.
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                chosenIndex = i;
                return points[i];
            }
        }

        return null;
    }

    private void HandleTeleportAttackEnded(Transform actor)
    {
        if (!restoreAfterAttack)
        {
            return;
        }

        if (!hasOriginalPose)
        {
            return;
        }

        transform.SetPositionAndRotation(originalPosition, originalRotation);

        if (logDebug)
        {
            Debug.Log($"CameraSwitchOnAttackTeleport: Restored camera after actor '{(actor != null ? actor.name : "(null)")}'.", this);
        }
    }
}
