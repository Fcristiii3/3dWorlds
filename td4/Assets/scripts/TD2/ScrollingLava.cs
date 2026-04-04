using UnityEngine;

[DisallowMultipleComponent]
public class ScrollingLava : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0.1f, 0.1f);

    private Renderer cachedRenderer;
    private Material cachedMaterial;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
        {
            cachedMaterial = cachedRenderer.material;
        }
    }

    private void Update()
    {
        if (cachedMaterial == null)
        {
            return;
        }

        cachedMaterial.mainTextureOffset = Time.time * scrollSpeed;
    }
}
