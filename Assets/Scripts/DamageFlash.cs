using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    public float flashDuration = 0.15f;
    
    private SkinnedMeshRenderer[] skinnedRenderers;
    private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock propBlock;

    private static readonly Color FLASH_COLOUR = new Color(1f, 0.7f, 0.7f, 1f);
    
    private Texture2D flashTexture;

    void Start()
    {
        skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();

        // Create a temporary 1x1 white texture in memory
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, FLASH_COLOUR);
        flashTexture.Apply();
    }

    public void TriggerFlash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

IEnumerator FlashRoutine()
{
    // 1. Force the color to white and swap the main texture to a solid white texture
    propBlock.SetColor("_Color", FLASH_COLOUR);
    propBlock.SetColor("_BaseColor", FLASH_COLOUR);
    
    // Unbinds the custom texture by replacing it with our solid white 1x1 texture
    propBlock.SetTexture("_MainTex", flashTexture);
    propBlock.SetTexture("_BaseMap", flashTexture);

    ApplyPropertyBlockToAll();

    // 2. Wait for the damage flash duration
    yield return new WaitForSeconds(flashDuration);

    // 3. Clear overrides to instantly snap back to textured appearance
    propBlock.Clear();
    ApplyPropertyBlockToAll();
}

    void ApplyPropertyBlockToAll()
    {
        foreach (var rend in skinnedRenderers)
        {
            rend.SetPropertyBlock(propBlock);
        }
        foreach (var rend in meshRenderers)
        {
            rend.SetPropertyBlock(propBlock);
        }
    }

    void OnDestroy()
    {
        // Clean up the generated texture from memory when object is destroyed
        if (flashTexture != null)
        {
            Destroy(flashTexture);
        }
    }
}