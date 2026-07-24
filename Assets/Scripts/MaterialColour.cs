using UnityEngine;

public class MaterialColour : MonoBehaviour
{
    [SerializeField] private Color _colour = Color.red;

    private static readonly int COLOUR_ID = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _propertyBlock;
    private Renderer[] _renderers;

    public Color colour {
        get => _propertyBlock.GetColor(COLOUR_ID);
        set
        {
            _colour = value;
            _propertyBlock.SetColor(COLOUR_ID, _colour);
            foreach (Renderer renderer in _renderers) {
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }

    void Start()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _renderers = GetComponentsInChildren<Renderer>();

        // Trigger the setter.
        colour = _colour;
    }
}