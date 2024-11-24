using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneName; // Name of the scene to load

    [Header("Interaction Settings")]
    public string playerTag = "Player"; // Tag assigned to the player's hand or interaction object

    [Header("Level Activation")]
    public bool isActive = true; // If false, the level selector is inactive

    private bool isLoading = false;
    private Renderer objectRenderer;
    private Material[] originalMaterials;

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // Create new material instances to avoid modifying shared materials
            Material[] mats = objectRenderer.materials;
            originalMaterials = new Material[mats.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                originalMaterials[i] = new Material(mats[i]);
                SetMaterialTransparent(originalMaterials[i]);
            }

            objectRenderer.materials = originalMaterials;
            UpdateObjectAppearance();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (isLoading)
            return;

        if (other.CompareTag(playerTag))
        {
            isLoading = true;
            LoadLevel();
        }
    }

    private void LoadLevel()
    {
        // Optionally, add any transition effects here
        SceneManager.LoadScene(sceneName);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        UpdateObjectAppearance();
    }

    private void UpdateObjectAppearance()
    {
        if (objectRenderer != null && originalMaterials != null)
        {
            foreach (Material mat in originalMaterials)
            {
                Color color = mat.color;
                if (isActive)
                {
                    color.a = 1.0f; // Fully opaque
                }
                else
                {
                    color.a = 0.5f; // 50% opacity
                }
                mat.color = color;
            }
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat != null)
        {
            // Change the rendering mode to Transparent
            mat.SetFloat("_Mode", 2); // 0: Opaque, 1: Cutout, 2: Fade, 3: Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
