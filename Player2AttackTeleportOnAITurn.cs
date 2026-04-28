using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Player2-only teleport + attack sequence triggered by TurnManager during AI turn.
/// This does not subscribe to HandManager and will not affect Player1 flow.
/// </summary>
public class Player2AttackTeleportOnAITurn : MonoBehaviour
{
    [Header("Player2")]
    [Tooltip("Optional. If empty, resolves Player2 from GameManager.")]
    [SerializeField] private Transform player2Root;

    [Tooltip("Optional. If empty, the script will try GetComponentInChildren<Animator>() from player2Root.")]
    [SerializeField] private Animator player2Animator;

    [Header("Teleport Target")]
    [Tooltip("Optional. If empty, this object's Transform will be used.")]
    [SerializeField] private Transform teleportPoint;

    [Tooltip("World-space offset applied after teleporting.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("Animation")]
    [Tooltip("Animator Trigger parameter name to fire the attack animation.")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Tooltip("Optional. If set, waits for this state to finish (layer 0). If empty, uses fallback duration.")]
    [SerializeField] private string attackStateName = "";

    [Tooltip("Used when Attack State Name is empty, or if the state can't be detected.")]
    [SerializeField] private float fallbackDurationSeconds = 0.8f;

    [Header("Behavior")]
    [Tooltip("If true, rotates Player2 to face this object after teleporting.")]
    [SerializeField] private bool faceTargetAfterTeleport = true;

    [Header("Facing / Inversion")]
    [Tooltip("If enabled, uses the same non-standard prefab forward-axis offset as TwoPlayerSpawner (project prefabs are not +Z forward).")]
    [SerializeField] private bool useProjectForwardAxisOffset = true;

    [Tooltip("Yaw offset (degrees) applied after LookRotation. Keep this in sync with TwoPlayerSpawner.LookRotationNegativeX().")]
    [SerializeField] private float lookYawOffsetDegrees = 60f;

    [Tooltip("Optional. If set, this transform's localScale.x sign will be preserved while the attack animation plays (prevents animation clips from un-flipping the model).")]
    [SerializeField] private Transform scaleLockRoot;

    [Tooltip("If true, enforces the initial localScale.x sign during the attack sequence.")]
    [SerializeField] private bool preserveLocalScaleXSignDuringAttack = false;

    [Tooltip("Prevents re-triggering while a sequence is already playing.")]
    [SerializeField] private bool ignoreWhileBusy = true;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private bool isBusy;
    private Coroutine sequenceRoutine;

    public void TriggerAttack()
    {
        if (ignoreWhileBusy && isBusy) return;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(AttackTeleportSequence(null, null));
    }

    public void TriggerAttack(Transform actorRoot, Animator actorAnimator = null)
    {
        if (actorRoot == null)
        {
            if (logDebug)
            {
                Debug.LogWarning("Player2AttackTeleportOnAITurn: TriggerAttack called with null actorRoot.", this);
            }
            return;
        }

        if (ignoreWhileBusy && isBusy) return;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(AttackTeleportSequence(actorRoot, actorAnimator));
    }

    private IEnumerator AttackTeleportSequence(Transform forcedPlayerRoot, Animator forcedAnimator)
    {
        isBusy = true;

        Transform resolvedPlayer = forcedPlayerRoot != null ? forcedPlayerRoot : ResolvePlayer2Root();
        if (resolvedPlayer == null)
        {
            if (logDebug)
            {
                Debug.LogWarning("Player2AttackTeleportOnAITurn: Could not resolve Player2 root.", this);
            }
            isBusy = false;
            yield break;
        }

        Animator resolvedAnimator = forcedAnimator != null ? forcedAnimator : ResolvePlayer2Animator(resolvedPlayer);
        Transform destination = teleportPoint != null ? teleportPoint : transform;

        Vector3 originalPosition = resolvedPlayer.position;
        Quaternion originalRotation = resolvedPlayer.rotation;

        // Preserve the actor's original "forward axis" basis so we don't visibly flip
        // when facing the target during the teleport attack (common when prefabs are
        // authored with a non-Unity forward, e.g. -X instead of +Z).
        Quaternion lookRotationOffset = Quaternion.identity;
        bool hasLookRotationOffset = false;
        if (faceTargetAfterTeleport)
        {
            Vector3 lookPoint = transform.position;
            Vector3 flatDirectionOriginal = lookPoint - originalPosition;
            flatDirectionOriginal.y = 0f;
            if (flatDirectionOriginal.sqrMagnitude > 0.0001f)
            {
                Quaternion baseRotOriginal = Quaternion.LookRotation(flatDirectionOriginal.normalized, Vector3.up);
                lookRotationOffset = Quaternion.Inverse(baseRotOriginal) * originalRotation;
                hasLookRotationOffset = true;
            }
        }

        Transform scaleTarget = ResolveScaleLockRoot(resolvedPlayer, resolvedAnimator);
        Vector3 originalScale = scaleTarget != null ? scaleTarget.localScale : Vector3.one;
        float originalScaleXSign = GetNonZeroSign(originalScale.x);

        CharacterController characterController = resolvedPlayer.GetComponent<CharacterController>();
        bool characterControllerWasEnabled = characterController != null && characterController.enabled;

        NavMeshAgent navMeshAgent = resolvedPlayer.GetComponent<NavMeshAgent>();
        bool navMeshAgentWasEnabled = navMeshAgent != null && navMeshAgent.enabled;

        Rigidbody rb = resolvedPlayer.GetComponent<Rigidbody>();
        bool rbWasKinematic = rb != null && rb.isKinematic;

        if (navMeshAgent != null && navMeshAgentWasEnabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }

        if (characterController != null && characterControllerWasEnabled)
        {
            characterController.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        resolvedPlayer.position = destination.position + worldOffset;

        if (faceTargetAfterTeleport)
        {
            Vector3 lookPoint = transform.position;
            Vector3 flatDirection = lookPoint - resolvedPlayer.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                resolvedPlayer.rotation = BuildFacingRotation(flatDirection.normalized, hasLookRotationOffset, lookRotationOffset);
            }
        }

        int preTriggerStateHash = 0;
        if (resolvedAnimator != null)
        {
            preTriggerStateHash = resolvedAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        }

        if (resolvedAnimator != null && !string.IsNullOrWhiteSpace(attackTriggerName))
        {
            resolvedAnimator.ResetTrigger(attackTriggerName);
            resolvedAnimator.SetTrigger(attackTriggerName);
        }
        else if (logDebug)
        {
            Debug.LogWarning("Player2AttackTeleportOnAITurn: No Animator found or trigger empty; using fallback wait.", this);
        }

        yield return WaitForAttackToFinish(
            resolvedAnimator,
            preTriggerStateHash,
            preserveLocalScaleXSignDuringAttack,
            scaleTarget,
            originalScaleXSign);

        resolvedPlayer.position = originalPosition;
        resolvedPlayer.rotation = originalRotation;

        if (scaleTarget != null)
        {
            scaleTarget.localScale = originalScale;
        }

        if (rb != null)
        {
            rb.isKinematic = rbWasKinematic;
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

        isBusy = false;
        sequenceRoutine = null;

        if (logDebug)
        {
            Debug.Log("Player2AttackTeleportOnAITurn: Sequence complete.", this);
        }
    }

    private Transform ResolvePlayer2Root()
    {
        if (player2Root != null) return player2Root;

        if (GameManager.Instance != null && GameManager.Instance.TryGetPlayerCharacters(out Character _, out Character p2))
        {
            if (p2 != null) return p2.transform;
        }

        return null;
    }

    private Animator ResolvePlayer2Animator(Transform resolvedPlayer)
    {
        if (player2Animator != null) return player2Animator;
        return resolvedPlayer.GetComponentInChildren<Animator>();
    }

    private IEnumerator WaitForAttackToFinish(
        Animator animator,
        int preTriggerStateHash,
        bool applyScaleLock,
        Transform scaleLockTarget,
        float lockedScaleXSign)
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(attackStateName))
        {
            const float enterTimeout = 1.0f;
            float enterElapsed = 0f;

            while (enterElapsed < enterTimeout)
            {
                if (applyScaleLock)
                {
                    ApplyScaleXSign(scaleLockTarget, lockedScaleXSign);
                }

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(attackStateName))
                {
                    break;
                }

                enterElapsed += Time.deltaTime;
                yield return null;
            }

            AnimatorStateInfo attackState = animator.GetCurrentAnimatorStateInfo(0);
            if (attackState.IsName(attackStateName))
            {
                const float finishTimeout = 5.0f;
                float finishElapsed = 0f;

                while (finishElapsed < finishTimeout)
                {
                    if (applyScaleLock)
                    {
                        ApplyScaleXSign(scaleLockTarget, lockedScaleXSign);
                    }

                    AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                    if (!current.IsName(attackStateName))
                    {
                        yield break;
                    }

                    if (current.normalizedTime >= 1f)
                    {
                        yield break;
                    }

                    finishElapsed += Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
                yield break;
            }
        }

        const float detectTimeout = 1.0f;
        float detectElapsed = 0f;
        int attackStateHash = 0;

        while (detectElapsed < detectTimeout)
        {
            if (applyScaleLock)
            {
                ApplyScaleXSign(scaleLockTarget, lockedScaleXSign);
            }

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
            yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
            yield break;
        }

        const float finishTimeoutAuto = 5.0f;
        float finishElapsedAuto = 0f;

        while (finishElapsedAuto < finishTimeoutAuto)
        {
            if (applyScaleLock)
            {
                ApplyScaleXSign(scaleLockTarget, lockedScaleXSign);
            }

            if (animator.IsInTransition(0))
            {
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
                    yield break;
                }

                if (current.normalizedTime >= 1f)
                {
                    yield break;
                }
            }

            finishElapsedAuto += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackDurationSeconds));
    }

    private Quaternion BuildFacingRotation(Vector3 normalizedFlatDirection, bool hasDynamicOffset, Quaternion dynamicOffset)
    {
        Quaternion baseRot = Quaternion.LookRotation(normalizedFlatDirection, Vector3.up);

        if (hasDynamicOffset)
        {
            return baseRot * dynamicOffset;
        }

        if (useProjectForwardAxisOffset)
        {
            baseRot *= Quaternion.Euler(0f, lookYawOffsetDegrees, 0f);
        }

        return baseRot;
    }

    private Transform ResolveScaleLockRoot(Transform resolvedPlayer, Animator resolvedAnimator)
    {
        if (scaleLockRoot != null) return scaleLockRoot;
        if (resolvedAnimator != null) return resolvedAnimator.transform;
        return resolvedPlayer;
    }

    private static float GetNonZeroSign(float value)
    {
        if (Mathf.Abs(value) < 0.0001f) return 1f;
        return Mathf.Sign(value);
    }

    private static void ApplyScaleXSign(Transform target, float sign)
    {
        if (target == null) return;

        Vector3 scale = target.localScale;
        float absX = Mathf.Abs(scale.x);
        if (absX < 0.0001f)
        {
            absX = 1f;
        }

        scale.x = absX * sign;
        target.localScale = scale;
    }
}
