using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("Vie")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Type d'Entité")]
    public bool isUnit = true;
    public bool isBuilding = false;
    public bool isExtractor = false;

    [Header("UI Vie")]
    public Canvas healthCanvas;
    public Slider healthBar;
    public Text healthText;

    [Header("Destruction")]
    public GameObject destructionEffect;
    public bool destroyOnDeath = true;

    void Start()
    {
        currentHealth = maxHealth;
        CreateHealthUI();
        UpdateHealthUI();
    }

    public void TakeDamage(int damage, GameObject attacker = null)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{gameObject.name} prend {damage} dégéts. Vie: {currentHealth}/{maxHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    void Die(GameObject killer = null)
    {
        Debug.Log($"{gameObject.name} est détruit !");

        // Effet de destruction
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Logique spécifique selon le type
        if (isExtractor)
        {
            // Libérer le ResourceNode
            ResourceNode[] nodes = FindObjectsOfType<ResourceNode>();
            foreach (var node in nodes)
            {
                if (Vector3.Distance(transform.position, node.transform.position) < 3f)
                {
                    Debug.Log($"ResourceNode {node.type} libéré !");
                    break;
                }
            }
        }

        if (isUnit)
        {
            // Réduire la population
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddPopulation(-1);
            }
        }

        // Détruire l'objet
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    void CreateHealthUI()
    {
        // Créer Canvas pour la barre de vie
        GameObject canvasGO = new GameObject("HealthCanvas");
        canvasGO.transform.SetParent(transform);

        healthCanvas = canvasGO.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.WorldSpace;
        healthCanvas.worldCamera = Camera.main;
        healthCanvas.sortingOrder = 5;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // TAILLE FIXE indépendante de l'objet
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3, 0.8f); // TAILLE FIXE

        // Position au-dessus - HAUTEUR FIXE
        canvasRect.localPosition = new Vector3(0, 2.5f, 0); // HAUTEUR FIXE
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one; // SCALE FIXE

        // Barre de vie
        GameObject healthBarGO = new GameObject("HealthBar");
        healthBarGO.transform.SetParent(canvasGO.transform, false);

        RectTransform healthBarRect = healthBarGO.AddComponent<RectTransform>();
        healthBar = healthBarGO.AddComponent<Slider>();

        // Configuration RectTransform - TAILLE FIXE
        healthBarRect.sizeDelta = new Vector2(3, 0.4f); // TAILLE FIXE
        healthBarRect.anchoredPosition = Vector2.zero;
        healthBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        healthBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        healthBarRect.pivot = new Vector2(0.5f, 0.5f);

        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarGO.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(healthBarGO.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        // Configuration du slider
        healthBar.fillRect = fillRect;
        healthBar.minValue = 0;
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;

        // Masquer par défaut
        healthCanvas.gameObject.SetActive(false);
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        // FORCER L'AFFICHAGE pour debug
        if (healthCanvas != null)
        {
            bool shouldShow = currentHealth < maxHealth || IsSelected();
            healthCanvas.gameObject.SetActive(shouldShow);

            // Debug
            if (shouldShow)
            {
                Debug.Log($"{gameObject.name} - Barre de vie active. HP: {currentHealth}/{maxHealth}");
            }
        }
    }

    // // Méthode pour tester l'affichage
    // void OnMouseDown()
    // {
    //     // Test : perdre des HP pour voir la barre
    //     TakeDamage(10);
    // }


    bool IsSelected()
    {
        // Vérifier si l'objet est sélectionné
        UnitMovement unit = GetComponent<UnitMovement>();
        if (unit != null && unit.IsSelected()) return true;

        Building building = GetComponent<Building>();
        if (building != null && building.isSelected) return true;

        return false;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}
