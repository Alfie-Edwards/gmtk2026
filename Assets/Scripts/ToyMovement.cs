using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ToyMovement : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The visual model child object that will bounce and tilt. Do not put this on the CharacterController root directly if it rotates the collider.")]
    [SerializeField] private Transform visualModel;

    [Header("Bobbing Settings (Vertical)")]
    [SerializeField] private float bobFrequency = 8.0f;
    [SerializeField] private float bobAmplitude = 0.1f;

    [Header("Tilting Settings (Rotation)")]
    [SerializeField] private float tiltFrequency = 4.0f;
    [SerializeField] private float maxTiltAngle = 10.0f;
    [Tooltip("The local axis about which the model wobbles/tilts.")]
    [SerializeField] private Vector3 tiltAxis = Vector3.forward;

    [Header("Smoothing")]
    [SerializeField] private float transitionSpeed = 10.0f;

    private CharacterController controller;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    
    private float currentMovementFactor = 0f;
    private float animationTimer = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (visualModel == null)
        {
            if (transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
                Debug.LogWarning("ToyBoxMovementAnimation: Visual Model not assigned. Defaulting to first child: " + visualModel.name, this);
            }
            else
            {
                Debug.LogError("ToyBoxMovementAnimation: No visual model assigned and no children found!", this);
                enabled = false;
                return;
            }
        }

        initialLocalPosition = visualModel.localPosition;
        initialLocalRotation = visualModel.localRotation;
    }

    private void Update()
    {
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.1f && controller.isGrounded;

        float targetFactor = isMoving ? 1f : 0f;
        currentMovementFactor = Mathf.Lerp(currentMovementFactor, targetFactor, Time.deltaTime * transitionSpeed);

        if (currentMovementFactor > 0.001f)
        {
            animationTimer += Time.deltaTime * (isMoving ? horizontalVelocity.magnitude : bobFrequency);

            float bobOffset = Mathf.Sin(animationTimer * bobFrequency) * bobAmplitude * currentMovementFactor;
            
            float tiltOffset = Mathf.Sin(animationTimer * tiltFrequency) * maxTiltAngle * currentMovementFactor;

            visualModel.localPosition = initialLocalPosition + new Vector3(0f, Mathf.Abs(bobOffset), 0f);
            visualModel.localRotation = initialLocalRotation * Quaternion.AngleAxis(tiltOffset, tiltAxis);
        }
        else
        {
            visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, initialLocalPosition, Time.deltaTime * transitionSpeed);
            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, initialLocalRotation, Time.deltaTime * transitionSpeed);
            animationTimer = 0f;
        }
    }
}