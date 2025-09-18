using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [Header("UI File de Production")]
    public Text productionQueueText;

    [Header("Ressources")]
    public int credits = 1000;
    public int energie = 500;
    public int population = 0;
    public int maxPopulation = 50;

    [Header("Revenus Passifs")]
    public int creditRevenue = 10; // Par seconde
    public int energieRevenue = 5;
    private float revenueTimer = 0f;

    [Header("UI Ressources")]
    public Text creditsText;
    public Text energieText;
    public Text populationText;

    [Header("Points de Ressources")]
    public GameObject resourceNodePrefab;
    public int initialNodeCount = 10;
    public float mapRadius = 50f;


    [Header("Singleton")]
    public static ResourceManager Instance;

    void Awake()
    {
        // Pattern Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CreateResourceUI();

        // Génération aléatoire des nodes
        for (int i = 0; i < initialNodeCount; i++)
        {
            Vector2 rand = Random.insideUnitCircle * mapRadius;
            Vector3 pos = new Vector3(rand.x, 0, rand.y);
            // Trouver position sur NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 100f, UnityEngine.AI.NavMesh.AllAreas))
            {
                GameObject node = Instantiate(resourceNodePrefab, hit.position, Quaternion.identity);
                // Alterner types
                node.GetComponent<ResourceNode>().type = (i % 2 == 0)
                    ? ResourceNode.ResourceType.Credits
                    : ResourceNode.ResourceType.Energie;
            }
        }

        UpdateUI();
    }

    void Update()
    {
        HandlePassiveIncome();
        UpdateUI();
        UpdateProductionQueue();
    }

    void HandlePassiveIncome()
    {
        revenueTimer += Time.deltaTime;

        if (revenueTimer >= 1f) // Chaque seconde
        {
            AddCredits(creditRevenue);
            AddEnergie(energieRevenue);
            revenueTimer = 0f;
        }
    }

    // Méthodes publiques pour gérer les ressources
    public bool CanAfford(int creditCost, int energieCost = 0, int populationCost = 0)
    {
        return credits >= creditCost &&
               energie >= energieCost &&
               (population + populationCost) <= maxPopulation;
    }

    public bool SpendResources(int creditCost, int energieCost = 0, int populationCost = 0)
    {
        if (CanAfford(creditCost, energieCost, populationCost))
        {
            credits -= creditCost;
            energie -= energieCost;
            population += populationCost;

            Debug.Log($"Ressources dépensées: {creditCost} crédits, {energieCost} énergie, +{populationCost} population");
            return true;
        }

        Debug.Log("Ressources insuffisantes!");
        return false;
    }

    public void AddCredits(int amount)
    {
        credits += amount;
        if (credits < 0) credits = 0;
    }

    public void AddEnergie(int amount)
    {
        energie += amount;
        if (energie < 0) energie = 0;
    }

    public void AddPopulation(int amount)
    {
        population += amount;
        if (population < 0) population = 0;
    }

    public void IncreaseMaxPopulation(int amount)
    {
        maxPopulation += amount;
    }

    void UpdateUI()
    {
        if (creditsText != null)
            creditsText.text = $"Crédits: {credits}";

        if (energieText != null)
            energieText.text = $"énergie: {energie}";

        if (populationText != null)
            populationText.text = $"Population: {population}/{maxPopulation}";
    }

    void CreateResourceUI()
{
    // Trouver ou créer le Canvas PRINCIPAL
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
        GameObject canvasGO = new GameObject("Main UI Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
    
    // Vérifier si le panel ressources existe déjà
    Transform existingPanel = canvas.transform.Find("ResourcePanel");
    if (existingPanel != null)
    {
        Debug.Log("ResourcePanel existe déjà");
        // Récupérer les références existantes
        Text[] texts = existingPanel.GetComponentsInChildren<Text>();
        if (texts.Length >= 3)
        {
            creditsText = texts[0];
            energieText = texts[1];
            populationText = texts[2];
        }
        return;
    }
    
    // Créer panel pour les ressources SEULEMENT s'il n'existe pas
    GameObject resourcePanelGO = new GameObject("ResourcePanel");
    resourcePanelGO.transform.SetParent(canvas.transform, false);
    
    RectTransform panelRect = resourcePanelGO.AddComponent<RectTransform>();
    Image panelImage = resourcePanelGO.AddComponent<Image>();
    
    // Position en haut à gauche - DIMENSIONS FIXES
    panelRect.anchorMin = new Vector2(0, 1);
    panelRect.anchorMax = new Vector2(0, 1);
    panelRect.pivot = new Vector2(0, 1);
    panelRect.anchoredPosition = new Vector2(10, -10);
    panelRect.sizeDelta = new Vector2(300, 100);
    
    panelImage.color = new Color(0, 0, 0, 0.7f);
    
    // Layout vertical
    VerticalLayoutGroup layoutGroup = resourcePanelGO.AddComponent<VerticalLayoutGroup>();
    layoutGroup.padding = new RectOffset(10, 10, 10, 10);
    layoutGroup.spacing = 5;
    layoutGroup.childAlignment = TextAnchor.UpperLeft;
    
    // Créer les textes des ressources
    creditsText = CreateResourceText("Crédits: 1000", resourcePanelGO);
    energieText = CreateResourceText("Énergie: 500", resourcePanelGO);
    populationText = CreateResourceText("Population: 0/50", resourcePanelGO);
}



    Text CreateResourceText(string initialText, GameObject parent)
    {
        GameObject textGO = new GameObject("ResourceText");
        textGO.transform.SetParent(parent.transform);

        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = initialText;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 16;
        textComponent.color = Color.white;

        return textComponent;
    }

    // Nouvelle méthode pour mettre à jour la file
    public void UpdateProductionQueue()
    {
        Building[] buildings = FindObjectsOfType<Building>();
        string queueInfo = "File: ";

        foreach (Building building in buildings)
        {
            if (building.isSelected && building.productionQueue.Count > 0)
            {
                queueInfo += $"{building.productionQueue.Count} unités";
                break;
            }
        }

        if (queueInfo == "File: ") queueInfo = "File: Vide";

        if (productionQueueText != null)
            productionQueueText.text = queueInfo;
    }
}
