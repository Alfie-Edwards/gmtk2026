using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System.Collections;

public class Horse : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

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
            if (!restarting && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
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

        float moveForwardAmount = 0;
        float moveRightAmount = 0;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) moveForwardAmount += 1;
            if (Keyboard.current.downArrowKey.isPressed) moveForwardAmount -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) moveRightAmount += 1;
            if (Keyboard.current.leftArrowKey.isPressed) moveRightAmount -= 1;
        }
        Vector3 move = ((Vector3.forward * moveForwardAmount) + (Vector3.right * moveRightAmount)).normalized * moveSpeed;

        // Jump
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move((velocity + move + knockbackVelocity) * Time.deltaTime);

        if (move != Vector3.zero)
        {
            lookTarget = Quaternion.LookRotation(move);
        }
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