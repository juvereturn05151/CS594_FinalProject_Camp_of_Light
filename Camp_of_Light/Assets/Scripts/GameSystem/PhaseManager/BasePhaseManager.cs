using UnityEngine;
using System.IO;
using UnityEngine.UI;

public abstract class BasePhaseManager : MonoBehaviour, IPhaseManager
{
    [Header("Sprite Settings")]
    [SerializeField] private float pixelsPerUnit = 100f;

    public abstract GamePhase Phase { get; }

    public abstract void EnterPhase(GameRunState state);

    public virtual void ExitPhase()
    {
        gameObject.SetActive(false);
    }

    protected static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }

    protected static void SetActive(Component target, bool value)
    {
        if (target != null)
            target.gameObject.SetActive(value);
    }

    protected void ApplyGeneratedSprites(GameRunState state, SpriteRenderer player, SpriteRenderer spirit)
    {
        Debug.Log("[ConsciencePhaseManager] Applying generated sprites from saved state.");
        if (state == null || state.Profile == null)
        {
            Debug.LogWarning("[ConsciencePhaseManager] State or profile is null.");
            return;
        }

        if (player != null && !string.IsNullOrWhiteSpace(state.Profile.PlayerCharacterImagePath))
        {
            Sprite playerSprite = LoadSpriteFromPath(state.Profile.PlayerCharacterImagePath);
            if (playerSprite != null)
            {
                Debug.Log("[ConsciencePhaseManager] Successfully loaded player sprite from saved path.");
                player.sprite = playerSprite;
            }
            else
            {
                Debug.LogWarning("[ConsciencePhaseManager] Failed to load player sprite from saved path.");
            }
        }

        if (spirit != null && !string.IsNullOrWhiteSpace(state.Profile.SpiritCharacterImagePath))
        {
            Sprite spiritSprite = LoadSpriteFromPath(state.Profile.SpiritCharacterImagePath);
            if (spiritSprite != null)
            {
                Debug.Log("[ConsciencePhaseManager] Successfully loaded spirit sprite from saved path.");
                spirit.sprite = spiritSprite;
            }
            else
            {
                Debug.LogWarning("[ConsciencePhaseManager] Failed to load spirit sprite from saved path.");
            }
        }
    }

    protected void ApplyGeneratedSprites(GameRunState state, Image player, Image spirit)
    {
        Debug.Log("[ConsciencePhaseManager] Applying generated sprites from saved state.");
        if (state == null || state.Profile == null)
        {
            Debug.LogWarning("[ConsciencePhaseManager] State or profile is null.");
            return;
        }

        if (player != null && !string.IsNullOrWhiteSpace(state.Profile.PlayerCharacterImagePath))
        {
            Sprite playerSprite = LoadSpriteFromPath(state.Profile.PlayerCharacterImagePath);
            if (playerSprite != null)
            {
                Debug.Log("[ConsciencePhaseManager] Successfully loaded player sprite from saved path.");
                player.sprite = playerSprite;
            }
            else
            {
                Debug.LogWarning("[ConsciencePhaseManager] Failed to load player sprite from saved path.");
            }
        }

        if (spirit != null && !string.IsNullOrWhiteSpace(state.Profile.SpiritCharacterImagePath))
        {
            Sprite spiritSprite = LoadSpriteFromPath(state.Profile.SpiritCharacterImagePath);
            if (spiritSprite != null)
            {
                Debug.Log("[ConsciencePhaseManager] Successfully loaded spirit sprite from saved path.");
                spirit.sprite = spiritSprite;
            }
            else
            {
                Debug.LogWarning("[ConsciencePhaseManager] Failed to load spirit sprite from saved path.");
            }
        }
    }

    protected Sprite LoadSpriteFromPath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Debug.LogWarning("[ConsciencePhaseManager] Image path is empty.");
            return null;
        }

        if (!File.Exists(imagePath))
        {
            Debug.LogWarning($"[ConsciencePhaseManager] Image file does not exist: {imagePath}");
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogWarning($"[ConsciencePhaseManager] Image file is empty: {imagePath}");
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = texture.LoadImage(imageBytes);

        if (!loaded)
        {
            Debug.LogWarning($"[ConsciencePhaseManager] Failed to decode image: {imagePath}");
            Object.Destroy(texture);
            return null;
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }
}