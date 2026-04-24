using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Attach this to a target object (e.g., the enemy). When an Attack card is confirmed,
/// it teleports the player to this object, triggers an attack animation, then returns
/// the player to their original position.
/// </summary>
public class AttackTeleportOnAttackCardConfirm : MonoBehaviour
{
    [Header("Event Source")]
    [Tooltip("Optional. If empty, the script will FindObjectOfType<HandManager>() on enable.")]
    [SerializeField] private HandManager handManager;

    [Header("Player")]
    [Tooltip("Optional. If empty, the script will resolve Player1 via GameManager/HPTrackerBinder.")]
    [SerializeField] private Transform playerRoot;

    [Tooltip("Optional. If empty, the script will try GetComponentInChildren<Animator>() from the playerRoot.")]
    [SerializeField] private Animator playerAnimator;

    [Header("Teleport Target")]
    [Tooltip("Optional. If empty, this object's Transform will be used.")]
    [SerializeField] private Transform teleportPoint;

    [Tooltip("World-space offset applied after teleporting.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("Animation")]
    [Tooltip("Animator Trigger parameter name to fire the attack animation.")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Optional. If set, the script will wait for this state to finish (layer 0). If empty, uses Fallback Duration.")]
    [SerializeField] private string attackStateName = "";

    [Tooltip("Used when Attack State Name is empty, or if the state can't be detected.")]
    [SerializeField] private float fallbackDurationSeconds = 0.8f;

    [Header("Behavior")]
    [Tooltip("If true, rotates the player to face this object after teleporting.")]
    [SerializeField] private bool faceTargetAfterTeleport = true;

    [Tooltip("Prevents re-triggering while an attack sequence is already playing.")]
    [SerializeField] private bool ignoreWhileBusy = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private bool _isBusy;
    private Coroutine _sequenceRoutine;

    private void OnEnable()
    {
        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }

        if (handManager != null)
        {
            handManager.OnCardConfirmed -= HandleCardConfirmed;
            handManager.OnCardConfirmed += HandleCardConfirmed;
        }
        else if (logDebug)
        {
            Debug.LogWarning("AttackTeleportOnAttackCardConfirm: No HandManager found; won't listen for card confirms.", this);
        }
    }

    private void OnDisable()
    {
        if (handManager != null)
        {
            handManager.OnCardConfirmed -= HandleCardConfirmed;
        }

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        _isBusy = false;
    }

    private void HandleCardConfirmed(CardInspector inspector, CardData card)
    {
        if (card == null) return;
        if (card.Type != CardType.Attack) return;

        if (ignoreWhileBusy && _isBusy) return;

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(AttackTeleportSequence());
    }

    private IEnumerator AttackTeleportSequence()
    {
        _isBusy = true;

        Transform resolvedPlayer = ResolvePlayerRoot();
        if (resolvedPlayer == null)
        {
            if (logDebug)
            {
                Debug.LogWarning("AttackTeleportOnAttackCardConfirm: Could not resolve playerRoot.", this);
            }
            _isBusy = false;
            yield break;
        }

        Animator resolvedAnimator = ResolvePlayerAnimator(resolvedPlayer);
        Transform destination = teleportPoint != null ? teleportPoint : transform;

        Vector3 originalPosition = resolvedPlayer.position;
        Quaternion originalRotation = resolvedPlayer.rotation;

        // Temporarily disable common movement/physics components to avoid fighting the teleport.
        CharacterController characterController = resolvedPlayer.GetComponent<CharacterController>();
        bool characterControllerWasEnabled = characterController != null && characterController.enabled;

        NavMeshAgent navMeshAgent = resolvedPlayer.GetComponent<NavMeshAgent>();
        bool navMeshAgentWasEnabled = navMeshAgent != null && navMeshAgent.enabled;

        Rigidbody rigidbody = resolvedPlayer.GetComponent<Rigidbody>();
        bool rigidbodyWasKinematic = rigidbody != null && rigidbody.isKinematic;

        if (navMeshAgent != null && navMeshAgentWasEnabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        if (characterController != null && characterControllerWasEnabled)
        {
            characterController.enabled = false;
        }

        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        // Teleport to target.
        resolvedPlayer.position = destination.position + worldOffset;

        if (faceTargetAfterTeleport)
        {
            Vector3 lookPoint = transform.position;
            Vector3 flatDirection = lookPoint - resolvedPlayer.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                resolvedPlayer.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            }
        }

        int preTriggerStateHash = 0;
        if (resolvedAnimator != null)
        {
            preTriggerStateHash = resolvedAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        }

        // Trigger the attack animation.
        if (resolvedAnimator != null && !string.IsNullOrWhiteSpace(attackTriggerName))
        {
            resolvedAnimator.ResetTrigger(attackTriggerName);
            resolvedAnimator.SetTrigger(attackTriggerName);
        }
        else if (logDebug)
        {
            Debug.LogWarning("AttackTeleportOnAttackCardConfirm: No Animator found or trigger name empty; using fallback wait.", this);
        }

        // Wait for animation.
        yield return WaitForAttackToFinish(resolvedAnimator, preTriggerStateHash);

        // Return player.
        resolvedPlayer.position = originalPosition;
        resolvedPlayer.rotation = originalRotation;

        // Restore movement/physics.
        if (rigidbody != null)
        {
            rigidbody.isKinematic = rigidbodyWasKinematic;
        }

        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = navMeshAgentWasEnabled;
            if (navMeshAgentWasEnabled)
            {
                navMeshAgent.isStopped = false;
            }
        }

        _isBusy = false;
        _sequenceRoutine = null;

        if (logDebug)
        {
            Debug.Log("AttackTeleportOnAttackCardConfirm: Attack teleport sequence complete.", this);
        }
    }

    private Transform ResolvePlayerRoot()
    {
        if (playerRoot != null) return playerRoot;

        // Prefer GameManager's resolution for Player1.
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.TryGetPlayerCharacters(out Character p1, out Character _))
            {
                if (p1 != null) return p1.transform;
            }
        }

        // Fallback: find any Character in scene.
        Character anyCharacter = FindObjectOfType<Character>();
        if (anyCharacter != null) return anyCharacter.transform;

        return null;
    }

    private Animator ResolvePlayerAnimator(Transform resolvedPlayer)
    {
        if (playerAnimator != null) return playerAnimator;

        Animator anim = resolvedPlayer.GetComponentInChildren<Animator>();
        if (anim != null) return anim;

        return null;
    }

    private IEnumerator WaitForAttackToFinish(Animator animator, int preTriggerStateHash)
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
            yield break;
        }

        // If the user specified a state name, try to wait for it to play fully.
        if (!string.IsNullOrWhiteSpace(attackStateName))
        {
            const float enterTimeout = 1.0f;
            float enterElapsed = 0f;

            while (enterElapsed < enterTimeout)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(attackStateName))
                {
                    break;
                }

                enterElapsed += Time.deltaTime;
                yield return null;
            }

            // If we're in the attack state, wait until it finishes (normalizedTime >= 1).
            AnimatorStateInfo attackState = animator.GetCurrentAnimatorStateInfo(0);
            if (attackState.IsName(attackStateName))
            {
                const float finishTimeout = 5.0f;
                float finishElapsed = 0f;

                while (finishElapsed < finishTimeout)
                {
                    AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                    if (!current.IsName(attackStateName))
                    {
                        // Transitioned away.
                        yield break;
                    }

                    if (current.normalizedTime >= 1f)
                    {
                        yield break;
                    }

                    finishElapsed += Time.deltaTime;
                    yield return null;
                }

                // Timed out; fall back to a small wait to avoid immediate snap-back.
                yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
                yield break;
            }
        }

        // Auto-detect the state that played after the trigger.
        // This works well when the trigger causes a transition into a distinct "attack" state.
        // If we can't reliably detect it, we fall back to a fixed wait to avoid infinite hangs.
        const float detectTimeout = 1.0f;
        float detectElapsed = 0f;

        int attackStateHash = 0;
        while (detectElapsed < detectTimeout)
        {
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.fullPathHash != 0 && next.fullPathHash != preTriggerStateHash)
                {
                    attackStateHash = next.fullPathHash;
                    break;
                }
            }

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.fullPathHash != 0 && current.fullPathHash != preTriggerStateHash)
            {
                attackStateHash = current.fullPathHash;
                break;
            }

            detectElapsed += Time.deltaTime;
            yield return null;
        }

        if (attackStateHash == 0)
        {
            // Fallback: simple duration.
            yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
            yield break;
        }

        const float finishTimeoutAuto = 5.0f;
        float finishElapsedAuto = 0f;

        while (finishElapsedAuto < finishTimeoutAuto)
        {
            if (animator.IsInTransition(0))
            {
                // If we're transitioning away from the detected attack state, consider it finished.
                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                bool currentIsAttack = current.fullPathHash == attackStateHash;
                bool nextIsAttack = next.fullPathHash == attackStateHash;
                if (!currentIsAttack && !nextIsAttack)
                {
                    yield break;
                }
            }
            else
            {
                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                if (current.fullPathHash != attackStateHash)
                {
                    // Left the attack state.
                    yield break;
                }

                // Wait for a non-looping clip to complete.
                if (current.normalizedTime >= 1f)
                {
                    yield break;
                }
            }

            finishElapsedAuto += Time.deltaTime;
            yield return null;
        }

        // Timed out; fall back to a small wait to avoid immediate snap-back.
        yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
    }
}
