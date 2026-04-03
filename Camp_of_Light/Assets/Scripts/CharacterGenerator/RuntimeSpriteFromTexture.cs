using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RuntimeSpriteFromTexture : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private float pixelsPerUnit = 100f;
    [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);

    private Sprite currentSprite;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    public void ApplyTexture(Texture2D texture)
    {
        if (texture == null || targetRenderer == null)
            return;

        if (currentSprite != null)
            Destroy(currentSprite);

        currentSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            pivot,
            pixelsPerUnit
        );

        targetRenderer.sprite = currentSprite;
    }
}