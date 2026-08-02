using System;
using System.Collections;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

public class EnemyLava : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;

    [Header("Blend Shape Animation")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    [Tooltip("Exact name of the fully-down blend shape.")]
    [SerializeField] private string squashBlendShapeName = "squash";

    [Tooltip("Exact name of the fully-up blend shape.")]
    [SerializeField] private string stretchBlendShapeName = "stretch";

    [Tooltip("How quickly the blend-shape weights change per second.")]
    [SerializeField] private float transitionSpeed = 2500f;

    [SerializeField] private float rotationSpeed = 10f;

    [Header("Collision")]
    [SerializeField] private CapsuleCollider attackCollider;

    [Tooltip("Collider centre when fully underground.")]
    [SerializeField] private Vector3 hiddenColliderCenter = Vector3.zero;

    [Tooltip("Collider height when fully underground.")]
    [SerializeField] private float hiddenColliderHeight = 0.1f;

    [Tooltip("Collider centre when fully extended.")]
    [SerializeField]
    private Vector3 poppedColliderCenter =
        new Vector3(0f, 1f, 0f);

    [Tooltip("Collider height when fully extended.")]
    [SerializeField] private float poppedColliderHeight = 2f;

    [Tooltip("Disable the collider when the monster is fully underground.")]
    [SerializeField] private bool disableColliderWhenHidden = true;

    [Header("Combat Settings")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private Transform firePoint;

    [Tooltip("Time to wait after emerging before shooting.")]
    [SerializeField] private float popWaitTime = 1f;

    [Tooltip("Time to wait after hiding before emerging again.")]
    [SerializeField] private float duckWaitTime = 2f;

    private int squashBlendShapeIndex = -1;
    private int stretchBlendShapeIndex = -1;

    private bool isActionRoutineRunning;
    private Player cachedPlayer;

    private Player PlayerInstance
    {
        get
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<Player>();
            }

            return cachedPlayer;
        }
    }

    private void Awake()
    {
        FindBlendShapeIndices();
    }

    private void Start()
    {
        // Start fully underground:
        // squash = 100, stretch = 0
        SetPopAmount(0f);
        transitionSpeed = UnityEngine.Random.Range(500f, 1000f);
    }

    private void Update()
    {
        Player player = PlayerInstance;

        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        if (distanceToPlayer > detectionRadius)
        {
            return;
        }

        FacePlayer(player.transform);

        if (!isActionRoutineRunning)
        {
            StartCoroutine(LavaRoutine());
        }
    }

    private void FindBlendShapeIndices()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError(
                $"{name}: No SkinnedMeshRenderer has been assigned.",
                this
            );

            return;
        }

        Mesh mesh = skinnedMeshRenderer.sharedMesh;

        if (mesh == null)
        {
            Debug.LogError(
                $"{name}: The SkinnedMeshRenderer has no mesh.",
                this
            );

            return;
        }

        squashBlendShapeIndex =
            mesh.GetBlendShapeIndex(squashBlendShapeName);

        stretchBlendShapeIndex =
            mesh.GetBlendShapeIndex(stretchBlendShapeName);

        if (squashBlendShapeIndex < 0)
        {
            Debug.LogError(
                $"{name}: Blend shape '{squashBlendShapeName}' was not found.",
                this
            );
        }

        if (stretchBlendShapeIndex < 0)
        {
            Debug.LogError(
                $"{name}: Blend shape '{stretchBlendShapeName}' was not found.",
                this
            );
        }
    }

    private void FacePlayer(Transform playerTransform)
    {
        Vector3 directionToPlayer =
            playerTransform.position - transform.position;

        // Only rotate around the Y axis.
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(directionToPlayer);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private IEnumerator LavaRoutine()
    {
        isActionRoutineRunning = true;

        // squash 100 -> 0
        // stretch 0 -> 100
        yield return AnimatePopAmount(1f);

        yield return new WaitForSeconds(popWaitTime);

        ShootSpike();

        // squash 0 -> 100
        // stretch 100 -> 0
        yield return AnimatePopAmount(0f);

        yield return new WaitForSeconds(duckWaitTime);

        isActionRoutineRunning = false;
    }

    private IEnumerator AnimatePopAmount(float targetAmount)
    {
        targetAmount = Mathf.Clamp01(targetAmount);

        float currentAmount = GetCurrentPopAmount();

        while (Mathf.Abs(currentAmount - targetAmount) > 0.001f)
        {
            currentAmount = Mathf.MoveTowards(
                currentAmount,
                targetAmount,
                transitionSpeed / 100f * Time.deltaTime
            );

            SetPopAmount(currentAmount);

            yield return null;
        }

        SetPopAmount(targetAmount);
    }

    private float GetCurrentPopAmount()
    {
        if (skinnedMeshRenderer == null ||
            stretchBlendShapeIndex < 0)
        {
            return 0f;
        }

        float stretchWeight =
            skinnedMeshRenderer.GetBlendShapeWeight(
                stretchBlendShapeIndex
            );

        return Mathf.Clamp01(stretchWeight / 100f);
    }

    private void SetPopAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);

        // Both shapes overlap during the transition.
        float squashWeight = Mathf.Lerp(100f, 0f, amount);
        float stretchWeight = Mathf.Lerp(0f, 100f, amount);

        if (skinnedMeshRenderer != null)
        {
            if (squashBlendShapeIndex >= 0)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(
                    squashBlendShapeIndex,
                    squashWeight
                );
            }

            if (stretchBlendShapeIndex >= 0)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(
                    stretchBlendShapeIndex,
                    stretchWeight
                );
            }
        }

        UpdateCollider(amount);
    }

    private void UpdateCollider(float amount)
    {
        if (attackCollider == null)
        {
            return;
        }

        attackCollider.center = Vector3.Lerp(
            hiddenColliderCenter,
            poppedColliderCenter,
            amount
        );

        attackCollider.height = Mathf.Lerp(
            hiddenColliderHeight,
            poppedColliderHeight,
            amount
        );

        if (disableColliderWhenHidden)
        {
            attackCollider.enabled = amount > 0.01f;
        }
        else
        {
            attackCollider.enabled = true;
        }
    }

    private void ShootSpike()
    {
        if (spikePrefab == null || firePoint == null)
        {
            return;
        }

        GameObject spikeObject = Instantiate(
            spikePrefab,
            firePoint.position,
            transform.rotation
        );

        CactusSpike spike =
            spikeObject.GetComponent<CactusSpike>();

        if (spike != null)
        {
            spike.Initialize(transform.forward);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}


/*
using System.Collections;
using UnityEngine;

public class EnemyLava : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;

    [Header("Movement/Animation Settings")]
    public Transform visualRoot; // The part of the lava enemy that moves up and down
    public float hiddenYOffset = -2f; // Local Y position when ducked down
    public float poppedYOffset = 0f;  // Local Y position when popped up
    public float transitionSpeed = 5f;
    public float rotationSpeed = 10f; // How fast it turns to face the player

    [Header("Combat Settings")]
    public GameObject spikePrefab;
    public Transform firePoint;
    public float popWaitTime = 1f;   // Time to wait after popping up before shooting
    public float duckWaitTime = 2f;  // Cooldown time before it can pop up again

    private bool isActionRoutineRunning = false;
    private Player cachedPlayer;

    private Player PlayerInstance
    {
        get
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<Player>();
            }
            return cachedPlayer;
        }
    }

    void Start()
    {
        if (visualRoot != null)
        {
            // Start fully ducked down
            Vector3 pos = visualRoot.localPosition;
            pos.y = hiddenYOffset;
            visualRoot.localPosition = pos;
        }
    }

    void Update()
    {
        Player player = PlayerInstance;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            // Always turn to face the player on the Y axis while in range
            Vector3 directionToPlayer = player.transform.position - transform.position;
            directionToPlayer.y = 0f; // Keep rotation level
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Trigger the attack sequence if it's ready
            if (!isActionRoutineRunning)
            {
                StartCoroutine(LavaRoutine());
            }
        }
    }

    IEnumerator LavaRoutine()
    {
        isActionRoutineRunning = true;

        // 1. Pop up
        yield return StartCoroutine(MoveVisualY(poppedYOffset));

        // 2. Wait a bit
        yield return new WaitForSeconds(popWaitTime);

        // 3. Shoot cactus spike forward (in the direction the lava enemy is facing)
        ShootSpike();

        // 4. Duck back down
        yield return StartCoroutine(MoveVisualY(hiddenYOffset));

        // 5. Wait on cooldown before allowing another pop-up
        yield return new WaitForSeconds(duckWaitTime);

        isActionRoutineRunning = false;
    }

    IEnumerator MoveVisualY(float targetY)
    {
        if (visualRoot == null) yield break;

        while (Mathf.Abs(visualRoot.localPosition.y - targetY) > 0.01f)
        {
            Vector3 pos = visualRoot.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetY, transitionSpeed * Time.deltaTime);
            visualRoot.localPosition = pos;
            yield return null;
        }

        Vector3 finalPos = visualRoot.localPosition;
        finalPos.y = targetY;
        visualRoot.localPosition = finalPos;
    }

    void ShootSpike()
    {
        if (spikePrefab == null || firePoint == null) return;

        GameObject spikeObj = Instantiate(spikePrefab, firePoint.position, Quaternion.identity);
        CactusSpike spike = spikeObj.GetComponent<CactusSpike>();

        if (spike != null)
        {
            // Shoot straight forward relative to where the enemy is facing
            Vector3 shootDirection = transform.forward;
            spike.Initialize(shootDirection);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
*/