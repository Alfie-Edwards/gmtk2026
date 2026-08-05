using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public GameObject rockPrefab;
    public Transform rockSpawnPoint;
    private GameObject activeRock;
    public bool hasRock = true;

    [Header("Burrow Settings")]
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

        SpawnRock();
        ForceBurrowInstant();
    }

    void Update()
    {
        if (enemy == null) return;

        if (currentState is State.Burrowed or State.RespawningRock or State.Stunned)
        {
            if (controller != null && controller.enabled) controller.enabled = false;
            
            if (currentState != State.Stunned && currentState != State.RespawningRock)
            {
                SetEnemyY(GetGroundY() - burrowDepth);
            }
        }

        // Check for contact damage when burrowed or moving subsurface with an active rock
        if (hasRock && (currentState == State.Burrowed || currentState == State.SubSurfaceMoving))
        {
            CheckBurrowContactDamage();
        }

        if (hasRock && activeRock == null)
        {
            hasRock = false;
            
            if (activeBehaviorRoutine != null)
            {
                StopCoroutine(activeBehaviorRoutine);
                activeBehaviorRoutine = null;
            }

            StartCoroutine(TransitionToCircleRunFromBrokenRock());
        }

        HandleBehavior();
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

    private System.Collections.IEnumerator TransitionToCircleRunFromBrokenRock()
    {
        currentState = State.Emerging;
        if (controller != null) controller.enabled = false;

        float targetY = GetGroundY();
        while (Mathf.Abs((transform.position.y + enemyBaseOffset) - targetY) > 0.01f)
        {
            float newY = Mathf.MoveTowards(transform.position.y + enemyBaseOffset, targetY, burrowMoveSpeed * Time.deltaTime);
            SetEnemyY(newY);
            yield return null;
        }
        SetEnemyY(targetY);

        activeBehaviorRoutine = StartCoroutine(PerformCircleRunAndRespawnSequence());
    }

    float GetGroundY()
    {
        return groundTransform != null ? groundTransform.position.y : 0f;
    }

    Vector3 GetGroundCenterPosition()
    {
        if (groundTransform != null)
        {
            Vector3 pos = groundTransform.position;
            pos.y = transform.position.y;
            return pos;
        }
        return new Vector3(0f, transform.position.y, 0f);
    }

    void SetEnemyY(float targetY)
    {
        Vector3 pos = transform.position;
        pos.y = targetY - enemyBaseOffset;
        transform.position = pos;
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

    void ForceBurrowInstant()
    {
        if (controller != null) controller.enabled = false;
        SetEnemyY(GetGroundY() - burrowDepth);
    }

    void HandleBehavior()
    {
        if (currentState == State.Normal)
        {
            if (hasRock)
            {
                currentState = State.Burrowed;
            }
            else
            {
                if (activeBehaviorRoutine == null)
                {
                    activeBehaviorRoutine = StartCoroutine(PerformCircleRunAndRespawnSequence());
                }
            }
        }
        else if (currentState == State.Burrowed)
        {
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

    private System.Collections.IEnumerator PerformSubSurfaceMovesUntilFar()
    {
        currentState = State.Burrowed;
        while (hasRock)
        {
            Transform playerTransform = GetPlayerTransform();
            if (playerTransform == null) break;

            float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
            if (sqrDist > cactusTransitionRadius * cactusTransitionRadius)
            {
                break;
            }

            while (hasRock)
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

            if (!hasRock) yield break;

            playerTransform = GetPlayerTransform();
            if (playerTransform != null && (playerTransform.position - transform.position).sqrMagnitude > cactusTransitionRadius * cactusTransitionRadius)
            {
                break;
            }

            yield return StartCoroutine(ExecuteSubSurfaceMoveRoutine(GetPlayerTransform()?.position ?? transform.position));
            
            if (!hasRock) yield break;
        }

        if (!hasRock) yield break;

        currentState = State.CactusMove;
        activeBehaviorRoutine = StartCoroutine(PerformCactusSequenceWithCenterCheck());
        yield break;
    }

    private System.Collections.IEnumerator PerformCactusSequenceWithCenterCheck()
    {
        Vector3 centerPos = GetGroundCenterPosition();
        float distToCenter = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z));

        if (distToCenter > 3f)
        {
            while (hasRock)
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

            if (!hasRock) yield break;

            while (hasRock && Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z)) > 3f)
            {
                centerPos = GetGroundCenterPosition();
                Vector3 toCenter = centerPos - transform.position;
                toCenter.y = 0f;
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toCenter.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, burrowTrackingTurnSpeed * Time.deltaTime);
                }

                yield return StartCoroutine(ExecuteSubSurfaceMoveRoutine(centerPos, true));

                if (!hasRock) yield break;

                if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(centerPos.x, 0f, centerPos.z)) <= 3f)
                {
                    break;
                }
            }
        }

        if (!hasRock) yield break;

        yield return StartCoroutine(PerformCactusSequence());
    }

    private System.Collections.IEnumerator ExecuteSubSurfaceMoveRoutine(Vector3 targetPosition, bool moveTowardTargetPos = false)
    {
        currentState = State.SubSurfaceMoving;
        if (controller != null) controller.enabled = false;

        float floorY = GetGroundY();
        float baseBurrowY = floorY - burrowDepth;
        float peakRiseY = baseBurrowY + subSurfaceRiseHeight;

        float elapsed = 0f;
        while (elapsed < subSurfaceDuration)
        {
            if (!hasRock) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / subSurfaceDuration;

            float heightCurveT;
            if (t <= peakRiseTime)
            {
                heightCurveT = Mathf.Sin((t / peakRiseTime) * (Mathf.PI * 0.5f));
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
            transform.position += currentMoveDir * currentSpeed * Time.deltaTime;

            SetEnemyY(currentYEval);

            yield return null;
        }

        if (hasRock)
        {
            SetEnemyY(baseBurrowY);
            currentState = State.Burrowed;
        }
    }

    private System.Collections.IEnumerator PerformCactusSequence()
    {
        currentState = State.CactusMove;
        if (controller != null) controller.enabled = false;

        float floorY = GetGroundY();

        float cactusStartY = floorY - burrowDepth - cactusModelHeight - cactusBaseOffset;
        float cactusTargetY = floorY - cactusBaseOffset;

        float enemyStartY = floorY - burrowDepth;
        float enemyTargetY = floorY + cactusModelHeight;

        if (cactusPrefab != null)
        {
            activeCactus = Instantiate(cactusPrefab, new Vector3(transform.position.x, cactusStartY, transform.position.z), Quaternion.identity);
        }

        float progress = 0f;
        float totalDistance = cactusTargetY - cactusStartY;
        
        while (progress < totalDistance)
        {
            if (!hasRock)
            {
                if (activeCactus != null) Destroy(activeCactus);
                yield break;
            }

            if (activeCactus == null) break;

            float step = cactusRiseSpeed * Time.deltaTime;
            progress += step;
            float currentCactusY = Mathf.Min(cactusStartY + progress, cactusTargetY);
            float currentEnemyY = Mathf.Min(enemyStartY + (progress * (enemyTargetY - enemyStartY) / totalDistance), enemyTargetY);

            if (activeCactus != null)
            {
                activeCactus.transform.position = new Vector3(transform.position.x, currentCactusY, transform.position.z);
            }
            SetEnemyY(currentEnemyY);

            yield return null;
        }

        if (activeCactus != null)
        {
            activeCactus.transform.position = new Vector3(transform.position.x, cactusTargetY, transform.position.z);
            SetEnemyY(enemyTargetY);

            float waitTimer = 0f;
            while (waitTimer < cactusWaitTime)
            {
                if (!hasRock)
                {
                    if (activeCactus != null) Destroy(activeCactus);
                    yield break;
                }
                if (activeCactus == null) break;
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }

        if (activeCactus != null)
        {
            if (controller != null) controller.enabled = true;
            yield return StartCoroutine(ExecuteJumpTowardsPlayerRoutine(true));
        }
        else
        {
            hasRock = false;

            currentState = State.Stunned;
            float fallProgress = transform.position.y + enemyBaseOffset;
            float targetFallY = floorY - burrowDepth;

            while (Mathf.Abs(fallProgress - targetFallY) > 0.01f)
            {
                fallProgress = Mathf.MoveTowards(fallProgress, targetFallY, burrowMoveSpeed * Time.deltaTime);
                SetEnemyY(fallProgress);
                yield return null;
            }
            SetEnemyY(targetFallY);

            float stunTimer = 0f;
            while (stunTimer < cactusStunDuration)
            {
                stunTimer += Time.deltaTime;
                yield return null;
            }

            currentState = State.Emerging;
            if (controller != null) controller.enabled = false;

            float targetY = GetGroundY();
            while (Mathf.Abs((transform.position.y + enemyBaseOffset) - targetY) > 0.01f)
            {
                float newY = Mathf.MoveTowards(transform.position.y + enemyBaseOffset, targetY, burrowMoveSpeed * Time.deltaTime);
                SetEnemyY(newY);
                yield return null;
            }
            SetEnemyY(targetY);
            if (controller != null) controller.enabled = true;

            currentState = State.CircleRunning;
            activeBehaviorRoutine = StartCoroutine(PerformCircleRunAndRespawnSequence());
            yield break;
        }

        activeBehaviorRoutine = null;
    }

    private System.Collections.IEnumerator ExecuteJumpTowardsPlayerRoutine(bool reburrowOnLand)
    {
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

        while (controller != null && !controller.isGrounded)
        {
            if (!hasRock) yield break;
            controller.Move(jumpDirection * jumpForwardForce * Time.deltaTime);
            yield return null;
        }

        if (reburrowOnLand && hasRock)
        {
            yield return StartCoroutine(BurrowToDepthRoutine(burrowDepth));
            currentState = State.Burrowed;
        }
    }

    private System.Collections.IEnumerator BurrowToDepthRoutine(float depth)
    {
        if (controller != null) controller.enabled = false;
        float targetY = GetGroundY() - depth;
        while (Mathf.Abs((transform.position.y + enemyBaseOffset) - targetY) > 0.01f)
        {
            float newY = Mathf.MoveTowards(transform.position.y + enemyBaseOffset, targetY, burrowMoveSpeed * Time.deltaTime);
            SetEnemyY(newY);
            yield return null;
        }
        SetEnemyY(targetY);
    }

    private System.Collections.IEnumerator PerformCircleRunAndRespawnSequence()
    {
        currentState = State.CircleRunning;
        if (controller != null) controller.enabled = true;

        Vector3 centerPos = GetGroundCenterPosition();
        while (true)
        {
            centerPos = GetGroundCenterPosition();
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

            centerPos = GetGroundCenterPosition();
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

            if (controller != null)
            {
                controller.Move(transform.forward * circleRunSpeed * Time.deltaTime);
            }

            yield return null;
        }

        if (controller != null) controller.enabled = false;
        currentState = State.Jumping;
        
        enemy.Jump(maxUpwardsImpulse * 0.4f);
        yield return new WaitForSeconds(0.2f);

        currentState = State.RespawningRock;
        yield return StartCoroutine(BurrowToDepthRoutine(burrowDepth * 2f));

        SpawnRock();

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(BurrowToDepthRoutine(burrowDepth));
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