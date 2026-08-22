using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System.Collections;

public class Horse : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference anyKeyAction;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    [SerializeField] public float moveAcceleration = 12f;

    [Header("Riding & Proximity Detection")]
    public Transform mountPoint; // Define this in the Inspector where the player should sit
    public float detectionRadius = 3.0f; // Radius around mount point to check for proximity
    public LayerMask playerLayer; // Layer mask to filter the player
    public CanvasGroup endScreenGroup;
    public Image[] endScreenImages;
    public GameObject invisibleWall;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 knockbackVelocity = Vector3.zero;
    private Quaternion lookTarget;
    private Vector3 move;
    
    private bool isBeingRidden = false;
    private bool showingEndScreen = false;
    private bool endScreenFinished = false;
    private bool restarting = false;
    private GameObject rider;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        lookTarget = transform.rotation;
        move = Vector3.zero;
    }

    void Update()
    {
        if (!isBeingRidden)
        {
            CheckForRiderProximity();
            return;
        }
        if (endScreenFinished)
        {
            if (!restarting && anyKeyAction.action.WasPressedThisFrame())
            {
                StartCoroutine(Restart());
            }
            return;
        }

        if (!showingEndScreen && transform.position.z < 0 && transform.position.x > 25)
        {
            showingEndScreen = true;
            StartCoroutine(ShowEndScreen());
        }

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector3 moveTarget = ((Vector3.forward * moveInput.y) + (Vector3.right * moveInput.x)) * moveSpeed;
        move = Vector3.MoveTowards(move, moveTarget, moveAcceleration * Time.deltaTime);

        // make horse tokyo drift
        float turnFactor = 1f - Mathf.Abs(Vector3.Dot(move.normalized, moveTarget.normalized));
        move = Vector3.MoveTowards(move, move.normalized * moveSpeed, moveInput.magnitude * turnFactor * moveAcceleration * Time.deltaTime);

        if (jumpAction.action.WasPressedThisFrame() && controller.isGrounded)
        {
            velocity = Vector3.up * Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        if (!controller.isGrounded)
        {
            velocity += Vector3.up * gravity * Time.deltaTime;
        }

        Vector3 prevPos = transform.position;
        controller.Move((velocity + move) * Time.deltaTime);
        Vector3 delta = transform.position - prevPos;
        float xMove = delta.x / Time.deltaTime;
        float zMove = delta.z / Time.deltaTime;
        if (move.x > 0 && xMove < move.x) move.x = xMove < 0 ? 0 : xMove;
        if (move.x < 0 && xMove > move.x) move.x = xMove > 0 ? 0 : xMove;
        if (move.z > 0 && zMove < move.z) move.z = zMove < 0 ? 0 : zMove;
        if (move.z < 0 && zMove > move.z) move.z = zMove > 0 ? 0 : zMove;

        if (moveTarget != Vector3.zero)
        {
            lookTarget = Quaternion.LookRotation(moveTarget);
        }
        // Tilt when jumping
        lookTarget = Quaternion.Euler(controller.isGrounded ? 0f : velocity.y * -5f, lookTarget.eulerAngles.y, lookTarget.eulerAngles.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookTarget, Mathf.Min(4f * Time.deltaTime, 1f));
    }

    private IEnumerator ShowEndScreen()
    {
        Destroy(invisibleWall);
        float blackFadeTime = 3f;
        float imageFadeTime = 3f;

        if (endScreenImages != null)
        {
            Color colour = Color.white;
            colour.a = 0f;
            foreach (Image image in endScreenImages)
            {
                image.color = colour;
            }
        }

        float t0, elapsed;
        if (endScreenGroup != null)
        {
            t0 = Time.time;
            elapsed = 0f;
            while (elapsed < blackFadeTime)
            {
                yield return null;
                elapsed = Time.time - t0;
                endScreenGroup.alpha = Mathf.Clamp01(elapsed / blackFadeTime);
            }
            endScreenGroup.alpha = 1f;
        }

        if (endScreenImages != null)
        {
            Color colour = Color.clear;
            foreach (Image image in endScreenImages)
            {
                colour = image.color;
                t0 = Time.time;
                elapsed = 0f;
                while (elapsed < imageFadeTime)
                {
                    yield return null;
                    elapsed = Time.time - t0;
                    colour.a = Mathf.Clamp01(elapsed / imageFadeTime);
                    image.color = colour;
                }
                colour.a = 1f;
                image.color = colour;
                yield return new WaitForSeconds(1f);
            }
        }
        endScreenFinished = true;
    }

    private IEnumerator Restart()
    {
        restarting = true;
        float fadeTime = 0.5f;
        if (endScreenImages != null)
        {
            for (int i = endScreenImages.Length - 1; i >= 0; --i)
            {
                Color colour = endScreenImages[i].color;
                float t0 = Time.time;
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    yield return null;
                    elapsed = Time.time - t0;
                    colour.a = Mathf.Clamp01(1f - (elapsed / fadeTime));
                    endScreenImages[i].color = colour;
                }
                colour.a = 0f;
                endScreenImages[i].color = colour;
            }
        }
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Overworld");
    }

    private void CheckForRiderProximity()
    {
        if (mountPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(mountPoint.position, detectionRadius, playerLayer);

        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Player>() is Player player)
            {
                if (hit.transform.position.y >= mountPoint.position.y && player.velocity.y < 0)
                {
                    Ride(hit.gameObject);
                    break;
                }
            }
        }
    }

    public void Ride(GameObject candidate)
    {
        if (isBeingRidden || candidate == null) return;

        isBeingRidden = true;
        rider = candidate;

        if (rider.GetComponent<Player>() is Player player)
        {
            player.disableControls = true;
            player.cameraOffset += Vector3.up * 1.5f;
            player.moveSpeed = moveSpeed; // Camera follow logic uses player move speed.
        }

        if (rider.GetComponent<CharacterController>() is CharacterController cc)
        {
            cc.Move(Vector3.zero);
            cc.enabled = false;
        }

        if (mountPoint != null)
        {
            rider.transform.position = mountPoint.position;
            rider.transform.rotation = mountPoint.rotation;
            rider.transform.SetParent(mountPoint.transform);
        }
        else
        {
            rider.transform.SetParent(transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mountPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(mountPoint.position, detectionRadius);
        }
    }
}