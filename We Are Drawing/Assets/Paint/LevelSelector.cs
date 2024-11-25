using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelSelector : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneName; // Name of the scene to load

    [Header("Interaction Settings")]
    public string playerTag = "Player"; // Tag assigned to the player's hand or interaction object

    [Header("Level Activation")]
    public bool isActive = true; // If false, the level selector is inactive

    [Header("Opacity Settings")]
    [Range(0f, 1f)]
    public float inactiveOpacity = 0.5f; // Adjustable opacity when inactive (0 = fully transparent, 1 = fully opaque)

    private bool isLoading = false;

    // List to store all materials that need to be modified
    private List<Material> originalMaterials = new List<Material>();

    private void Start()
    {
        // Find all Renderer components in this GameObject and its children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Get all materials from the renderer
            Material[] mats = renderer.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                // Create a new instance of the material to avoid modifying shared materials
                Material matInstance = new Material(mats[i]);
                SetMaterialTransparent(matInstance);
                mats[i] = matInstance; // Replace with the new material instance

                // Add to the list for future updates
                originalMaterials.Add(matInstance);
            }

            // Assign the new materials back to the renderer
            renderer.materials = mats;
        }

        UpdateObjectAppearance();
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
        foreach (Material mat in originalMaterials)
        {
            if (mat != null)
            {
                Color color = mat.color;
                if (isActive)
                {
                    color.a = 1.0f; // Fully opaque
                }
                else
                {
                    color.a = inactiveOpacity; // Use the adjustable opacity value
                }
                mat.color = color;
            }
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat != null)
        {
            // Change the rendering mode to Fade or Transparent, depending on the shader
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2); // 0: Opaque, 1: Cutout, 2: Fade, 3: Transparent
            }

            if (mat.HasProperty("_Surface")) // For Universal Render Pipeline shaders
            {
                mat.SetFloat("_Surface", 1); // 0: Opaque, 1: Transparent
            }

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
