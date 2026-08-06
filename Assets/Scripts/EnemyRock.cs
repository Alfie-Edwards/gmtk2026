using UnityEngine;

using System;

[RequireComponent(typeof(Enemy))]
public class EnemyRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public GameObject rockPrefab;
    public Transform rockSpawnPoint;
    private GameObject activeRock;
    public bool hasRock = true;

    [Header("Burrow & Collision Settings")]
    [Tooltip("The Arena Floor layer.")]
    public MeshCollider arenaFloor;
    [Tooltip("The transform representing the ground surface.")]
    public Transform groundTransform;
    [Tooltip("Distance below the ground transform when burrowed.")]
    public float burrowDepth = 2f;
    [Tooltip("Speed when moving up or down during burrow transitions.")]
    public float burrowMoveSpeed = 10f;
    [Tooltip("Vertical offset from the enemy's transform origin to its feet/base.")]
    public float enemyBaseOffset = 0f;

    [Header("Contact Damage Settings")]
    [Tooltip("Radius for checking player contact while burrowed/subsurface moving.")]
    public float burrowContactRadius = 1.2f;
    [Tooltip("Layers to check for player contact.")]
    public LayerMask playerLayer;

    [Header("Cactus Settings")]
    public GameObject cactusPrefab;
    [Tooltip("The vertical height/size of the cactus model itself.")]
    public float cactusModelHeight = 2f;
    [Tooltip("Base offset to line up the cactus correctly at ground level.")]
    public float cactusBaseOffset = 0f;
    [Tooltip("Speed at which the cactus rises.")]
    public float cactusRiseSpeed = 6f;
    [Tooltip("Time the cactus stays extended before the enemy jumps off.")]
    public float cactusWaitTime = 0.75f;
    [Tooltip("Duration the enemy remains stunned if the cactus is destroyed underneath them.")]
    public float cactusStunDuration = 2f;
    private GameObject activeCactus;

    [Header("Detection & Charge Settings")]
    public float detectionRadius = 8f;
    public float cactusTransitionRadius = 12f;
    public float chargeSpeed = 8f;
    public float chargeTurnSpeed = 90f;
    public float initialTurnSpeed = 360f;
    public float chargeKnockbackModifier = 0.2f;
    public float maxChargeDuration = 1f;

    [Header("Circle Run Settings (No Rock)")]
    [Tooltip("Radius of the circle around the center that the enemy runs around.")]
    public float circleRunRadius = 4f;
    [Tooltip("Speed when running along the circle path.")]
    public float circleRunSpeed = 6f;
    [Tooltip("Turn speed while following the circle path.")]
    public float circleRunTurnSpeed = 180f;
    [Tooltip("Duration for which the enemy continuously runs around the circle.")]
    public float circleRunDuration = 8f;
    [Tooltip("How strongly the enemy pulls toward the circle perimeter to stay locked on the radius path.")]
    public float circleCorrectionStrength = 5f;

    [Header("Jump / Dash Settings")]
    public float jumpForwardForce = 8f;
    public float maxUpwardsImpulse = 12f;
    [Tooltip("Maximum height rise during the sub-surface approach movement (partial height).")]
    public float subSurfaceRiseHeight = 1f;
    [Tooltip("Maximum forward speed when deepest underground (most burrowed).")]
    public float subSurfaceMaxSpeed = 8f;
    [Tooltip("Minimum forward speed when closest to peak rise height (least burrowed).")]
    public float subSurfaceMinSpeed = 2f;
    [Tooltip("Duration of each sub-surface approach dash.")]
    public float subSurfaceDuration = 0.6f;
    [Tooltip("Normalized time (0 to 1) during the dash when the peak rise height occurs.")]
    [Range(0.1f, 0.9f)]
    public float peakRiseTime = 0.8f;
    [Tooltip("Turn speed when closest to peak rise height.")]
    public float burrowTrackingTurnSpeed = 120f;
    [Tooltip("Turn speed when deepest underground (most burrowed).")]
    public float burrowIdleTurnSpeed = 360f;
    [Tooltip("Angle threshold (in degrees) required to face the player before starting a new dash.")]
    public float dashFacingThreshold = 15f;

    private Enemy enemy;
    private CharacterController controller;
    private Vector3 jumpDirection;
    private Coroutine activeBehaviorRoutine;

    private enum State { Burrowed, Emerging, Normal, Charging, Jumping, SubSurfaceMoving, CactusMove, Stunned, RespawningRock, CircleRunning }
    private State currentState = State.Burrowed;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        controller = GetComponent<CharacterController>();
        currentState = State.Burrowed;
        StartCoroutine(BurrowToDepthRoutine(burrowDepth));

        SpawnRock();
    }

    void Update()
    {
        if (enemy == null) return;

        if (hasRock && (currentState == State.Burrowed || currentState == State.SubSurfaceMoving))
        {
            CheckBurrowContactDamage();
        }

        if (hasRock && activeRock == null)
        {
            hasRock = false;
            
            if (activeBehaviorRoutine != null)
            {
                Destroy(activeCactus);
                StopCoroutine(activeBehaviorRoutine);
                activeBehaviorRoutine = null;
            }

            StartCoroutine(PerformCircleRunAndRespawnSequence());
        }

        if (currentState == State.Burrowed)
        {
            DisableFloorCollisionAndGravity(true, true);
            if (activeBehaviorRoutine == null)
            {
                Transform playerTransform = GetPlayerTransform();
                if (playerTransform != null)
                {
                    float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
                    if (sqrDist <= detectionRadius * detectionRadius)
                    {
                        if (hasRock)
                        {
                            FindAnyObjectByType<HealthBar>()?.Show();
                            activeBehaviorRoutine = StartCoroutine(PerformSubSurfaceMovesUntilFar());
                        }
                        else
                        {
                            activeBehaviorRoutine = StartCoroutine(PerformCircleRunAndRespawnSequence());
                        }
                    }
                }
            }

        }
    }

    private void DisableFloorCollisionAndGravity(bool ignoreCollision, bool useZeroGravity)
    {
        Debug.Log($"Ignore collisions: {ignoreCollision}\nIgnore gravity: {useZeroGravity}");
        if (useZeroGravity) enemy.verticalVelocity = 0f;
        enemy.gravity = useZeroGravity ? 0f : 9.81f;
        Physics.IgnoreCollision(controller, arenaFloor, ignoreCollision);
    }

    private void CheckBurrowContactDamage()
    {
        Vector3 capsulePoint1 = transform.position + Vector3.up * enemyBaseOffset;
        Vector3 capsulePoint2 = transform.position + Vector3.up * (2f - enemyBaseOffset);
        float capsuleRadius = 0.27f;

        Collider[] bodyHits = Physics.OverlapCapsule(capsulePoint1, capsulePoint2, capsuleRadius, playerLayer);
        foreach (var hit in bodyHits)
        {
            if (hit.GetComponent<Player>() is Player player)
            {
                Debug.Log("HIT PLAYER (Body)");
                Vector3 knockbackDir = (player.transform.position - transform.position);
                knockbackDir.y *= 0.01f;
                knockbackDir.Normalize();
                player.GetHit(knockbackDir * 30);
                break;
            }
        }

        if (hasRock && activeRock != null)
        {
            Vector3 rockCenter = activeRock.transform.position;
            Vector3 cubeHalfExtents = new Vector3(0.35f, 0.35f, 0.35f);

            Collider[] rockHits = Physics.OverlapBox(rockCenter, cubeHalfExtents, activeRock.transform.rotation, playerLayer);
            foreach (var hit in rockHits)
            {
                if (hit.GetComponent<Player>() is Player player)
                {
                    Debug.Log("HIT PLAYER (Rock)");
                    Vector3 knockbackDir = (player.transform.position - activeRock.transform.position);
                    knockbackDir.y *= 0.01f;
                    knockbackDir.Normalize();
                    player.GetHit(knockbackDir * 30);
                    break;
                }
            }
        }
    }

    private System.Collections.IEnumerator Animate(float start, float end, float speed, Action<float> action, Func<bool> predicate=null)
    {
        action(start);
        float current = start;
        while (Mathf.Abs(end - current) > 0.01f)
        {
            if (predicate?.Invoke() ?? false)
            {
                yield break;
            }
            current = Mathf.MoveTowards(current, end, speed * Time.deltaTime);
            action(current);
            yield return null;
        }
        action(end);
    }

    void MoveEnemyToY(float targetFeetY)
    {
        float targetCenterY = targetFeetY - enemyBaseOffset;
        float currentCenterY = transform.position.y;
        controller.Move(new Vector3(0f, targetCenterY - currentCenterY, 0f));
    }

    void SpawnRock()
    {
        if (rockPrefab != null && activeRock == null)
        {
            Vector3 spawnPos = rockSpawnPoint != null ? rockSpawnPoint.position : transform.position + Vector3.up * 2f;
            activeRock = Instantiate(rockPrefab, spawnPos, transform.rotation, rockSpawnPoint ?? transform);
            hasRock = true;
        }
    }

    private System.Collections.IEnumerator PerformSubSurfaceMovesUntilFar()
    {
        Debug.Log("SUB SURFACE MOVES UNTIL FAR");
        currentState = State.Burrowed;
        while (true)
        {
            Transform playerTransform = GetPlayerTransform();
            if (playerTransform == null) break;

            float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
            if (sqrDist > cactusTransitionRadius * cactusTransitionRadius)
            {
                break;
            }

            while (true)
            {
                playerTransform = GetPlayerTransform();
                if (playerTransform == null) break;

                sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
                if (sqrDist > cactusTransitionRadius * cactusTransitionRadius)
                {
                    break;
                }

                Vector3 toPlayer = playerTransform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, burrowIdleTurnSpeed * Time.deltaTime);

                    float angleDiff = Quaternion.Angle(transform.rotation, targetRot);
                    if (angleDiff <= dashFacingThreshold)
                    {
                        break; 
                    }
                }

                yield return null;
            }
            playerTransform = GetPlayerTransform();
            if (playerTransform != null && (playerTransform.position - transform.position).sqrMagnitude > cactusTransitionRadius * cactusTransitionRadius)
            {
                break;
            }

            yield return ExecuteSubSurfaceMoveRoutine(GetPlayerTransform()?.position ?? transform.position);
        }

        currentState = State.CactusMove;
        activeBehaviorRoutine = StartCoroutine(PerformCactusSequence());
    }

    private System.Collections.IEnumerator PerformCactusSequence()
    {
        Debug.Log("CACTUS SEQUENCE");
        Vector3 centerPos = groundTransform.position;
        float distToCenter = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z));

        if (distToCenter > 3f)
        {
            while (true)
            {
                Vector3 toCenter = centerPos - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude <= 0.01f) break;

                Quaternion targetRot = Quaternion.LookRotation(toCenter.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, burrowIdleTurnSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, targetRot) <= dashFacingThreshold)
                {
                    break;
                }

                yield return null;
            }

            while (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z)) > 3f)
            {
                centerPos = groundTransform.position;
                Vector3 toCenter = centerPos - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCenter.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, burrowTrackingTurnSpeed * Time.deltaTime);
                }

                yield return ExecuteSubSurfaceMoveRoutine(centerPos, true);

                if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z)) <= 3f)
                {
                    break;
                }
            }
        }

        currentState = State.CactusMove;
        DisableFloorCollisionAndGravity(true, true);

        float cactusStartY = groundTransform.position.y - burrowDepth - cactusModelHeight - cactusBaseOffset;
        float cactusTargetY = groundTransform.position.y - cactusBaseOffset;

        if (cactusPrefab != null)
        {
            activeCactus = Instantiate(cactusPrefab, new Vector3(transform.position.x, cactusStartY, transform.position.z), Quaternion.identity);
        }

        yield return Animate(
            cactusStartY, 
            cactusTargetY, 
            cactusRiseSpeed, 
            currentCactusY => 
            {
                if (activeCactus != null)
                {
                    activeCactus.transform.position = new Vector3(transform.position.x, currentCactusY, transform.position.z);
                }
                MoveEnemyToY(currentCactusY + cactusModelHeight + cactusBaseOffset);
            }, 
            () => activeRock == null || activeCactus == null
        );

        if (activeCactus != null)
        {
            DisableFloorCollisionAndGravity(true, true);
            float waitTimer = 0f;
            while (waitTimer < cactusWaitTime)
            {
                if (activeRock == null)
                {
                    Destroy(activeCactus);
                    activeBehaviorRoutine = null;
                    yield break;
                }
                if (activeCactus == null) break;
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }

        if (activeCactus != null)
        {
            DisableFloorCollisionAndGravity(false, false);
            yield return ExecuteJumpTowardsPlayerRoutine(true);
        }
        else
        {
            currentState = State.Stunned;

            Debug.Log("FALLING TO STUN");
            yield return BurrowToDepthRoutine(burrowDepth);

            Debug.Log("STUNNED");
            yield return new WaitForSeconds(cactusStunDuration);

            Debug.Log("STUN FINNISHED");

        }

        currentState = State.Burrowed;
        yield return BurrowToDepthRoutine(burrowDepth);
        activeBehaviorRoutine = null;
    }

    private System.Collections.IEnumerator ExecuteSubSurfaceMoveRoutine(Vector3 targetPosition, bool moveTowardTargetPos = false)
    {
        Debug.Log("SUB SURFACE MOVE SEQUENCE");
        currentState = State.SubSurfaceMoving;
        yield return BurrowToDepthRoutine(burrowDepth);

        float baseBurrowY = groundTransform.position.y - burrowDepth;
        float peakRiseY = baseBurrowY + subSurfaceRiseHeight;

        float elapsed = 0f;
        while (elapsed < subSurfaceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / subSurfaceDuration;

            float heightCurveT;
            if (t <= peakRiseTime)
            {
                heightCurveT = Mathf.Sin(t * Mathf.PI * 0.5f / peakRiseTime);
            }
            else
            {
                float remainingT = (t - peakRiseTime) / (1f - peakRiseTime);
                heightCurveT = Mathf.Cos(remainingT * (Mathf.PI * 0.5f));
            }

            float currentYEval = Mathf.Lerp(baseBurrowY, peakRiseY, heightCurveT);
            float heightProgress = Mathf.InverseLerp(baseBurrowY, peakRiseY, currentYEval);

            if (moveTowardTargetPos)
            {
                Vector3 toTarget = targetPosition - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                    float blendedTurnSpeed = Mathf.Lerp(burrowIdleTurnSpeed, burrowTrackingTurnSpeed, heightProgress);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, blendedTurnSpeed * Time.deltaTime);
                }
            }
            else
            {
                Transform playerTransform = GetPlayerTransform();
                if (playerTransform != null)
                {
                    Vector3 toPlayer = playerTransform.position - transform.position;
                    toPlayer.y = 0f;
                    if (toPlayer.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                        float blendedTurnSpeed = Mathf.Lerp(burrowIdleTurnSpeed, burrowTrackingTurnSpeed, heightProgress);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, blendedTurnSpeed * Time.deltaTime);
                    }
                }
            }

            Vector3 currentMoveDir = transform.forward;
            float currentSpeed = Mathf.Lerp(subSurfaceMaxSpeed, subSurfaceMinSpeed, heightProgress);
            
            Vector3 translationDelta = currentMoveDir * currentSpeed * Time.deltaTime;
            float targetCenterY = currentYEval - enemyBaseOffset;
            translationDelta.y = targetCenterY - transform.position.y;
            
            controller.Move(translationDelta);
            yield return null;
        }

        currentState = State.Burrowed;
    }

    private System.Collections.IEnumerator ExecuteJumpTowardsPlayerRoutine(bool reburrowOnLand)
    {
        Debug.Log("JUMP TOWARDS PLAYER");
        currentState = State.Jumping;

        Transform playerTransform = GetPlayerTransform();
        if (playerTransform is not null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            jumpDirection = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
        }
        else
        {
            jumpDirection = transform.forward;
        }

        transform.rotation = Quaternion.LookRotation(jumpDirection);
        enemy.Jump(maxUpwardsImpulse);

        yield return new WaitForSeconds(0.05f);

        while (!controller.isGrounded)
        {
            controller.Move(jumpDirection * jumpForwardForce * Time.deltaTime);
            yield return null;
        }

        if (reburrowOnLand)
        {
            yield return BurrowToDepthRoutine(burrowDepth);
            currentState = State.Burrowed;
        }
    }

    private System.Collections.IEnumerator BurrowToDepthRoutine(float depth)
    {
        Debug.Log("BURROW");
        DisableFloorCollisionAndGravity(true, true);

        yield return Animate(
            transform.position.y + enemyBaseOffset,
            groundTransform.position.y - depth,
            burrowMoveSpeed,
            MoveEnemyToY
        );
    }

    private System.Collections.IEnumerator PerformCircleRunAndRespawnSequence()
    {
        Debug.Log("CIRCLE RUN");

        currentState = State.Emerging;
        yield return BurrowToDepthRoutine(groundTransform.position.y);
        DisableFloorCollisionAndGravity(false, false);
        currentState = State.CircleRunning;

        Vector3 centerPos = groundTransform.position;
        while (true)
        {
            centerPos = groundTransform.position;
            Vector3 currentPosFlat = new Vector3(transform.position.x, centerPos.y, transform.position.z);
            float distToCenter = Vector3.Distance(currentPosFlat, new Vector3(centerPos.x, centerPos.y, centerPos.z));

            if (Mathf.Abs(distToCenter - 3f) <= 0.2f) break;

            Vector3 toCenterDir = (centerPos - currentPosFlat);
            toCenterDir.y = 0f;
            if (toCenterDir.sqrMagnitude > 0.01f)
            {
                toCenterDir.Normalize();
                if (distToCenter > 3f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCenterDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, circleRunTurnSpeed * Time.deltaTime);
                    controller.Move(transform.forward * circleRunSpeed * Time.deltaTime);
                }
                else
                {
                    Quaternion targetRot = Quaternion.LookRotation(-toCenterDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, circleRunTurnSpeed * Time.deltaTime);
                    controller.Move(transform.forward * circleRunSpeed * Time.deltaTime);
                }
            }
            yield return null;
        }

        float runTimer = 0f;
        while (runTimer < circleRunDuration)
        {
            runTimer += Time.deltaTime;

            centerPos = groundTransform.position;
            Transform playerTransform = GetPlayerTransform();

            Vector3 playerDir = Vector3.forward;
            if (playerTransform != null)
            {
                playerDir = playerTransform.position - centerPos;
                playerDir.y = 0f;
                if (playerDir.sqrMagnitude > 0.01f)
                {
                    playerDir.Normalize();
                }
                else
                {
                    playerDir = Vector3.forward;
                }
            }

            Vector3 idealPoint = centerPos - playerDir * 3f;
            idealPoint.y = transform.position.y;

            Vector3 currentPosFlat = new Vector3(transform.position.x, centerPos.y, transform.position.z);
            Vector3 fromCenter = currentPosFlat - centerPos;
            fromCenter.y = 0f;
            
            Vector3 tangentDir = Vector3.Cross(Vector3.up, fromCenter);
            if (tangentDir.sqrMagnitude > 0.01f)
            {
                tangentDir.Normalize();
                Vector3 toIdeal = idealPoint - currentPosFlat;
                toIdeal.y = 0f;
                if (Vector3.Dot(tangentDir, toIdeal) < 0f)
                {
                    tangentDir = -tangentDir;
                }
            }
            else
            {
                tangentDir = transform.forward;
            }

            float currentRadius = fromCenter.magnitude;
            Vector3 radiusCorrection = Vector3.zero;
            if (currentRadius > 0.01f)
            {
                float radiusError = 3f - currentRadius;
                radiusCorrection = (fromCenter / currentRadius) * radiusError * circleCorrectionStrength;
            }

            Vector3 moveDir = (tangentDir + radiusCorrection).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, circleRunTurnSpeed * Time.deltaTime);
            }

            controller.Move(transform.forward * circleRunSpeed * Time.deltaTime);

            yield return null;
        }

        currentState = State.Jumping;
        DisableFloorCollisionAndGravity(true, false);
        
        MoveEnemyToY(groundTransform.position.y);

        enemy.Jump(maxUpwardsImpulse * 0.4f);
        yield return new WaitForSeconds(0.2f);

        currentState = State.RespawningRock;
        yield return BurrowToDepthRoutine(burrowDepth * 2f);

        SpawnRock();

        yield return new WaitForSeconds(1f);

        yield return BurrowToDepthRoutine(burrowDepth);
        currentState = State.Burrowed;
        activeBehaviorRoutine = null;
    }

    private Transform GetPlayerTransform()
    {
        Player playerComp = FindAnyObjectByType<Player>();
        return playerComp is not null ? playerComp.transform : null;
    }

    public bool CanTakeDamage()
    {
        return !hasRock || activeRock == null;
    }

    public float GetModifiedKnockback(float baseKnockback)
    {
        if (hasRock && activeRock != null)
        {
            return baseKnockback * chargeKnockbackModifier;
        }
        return baseKnockback;
    }
}