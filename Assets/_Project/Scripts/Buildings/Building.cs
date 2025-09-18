using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class UnitType
{
    public string name;
    public GameObject prefab;
    public int cost;
    public float productionTime;
    public KeyCode shortcut;
}

public class Building : MonoBehaviour
{
    public bool isExtractor = false;
    public ResourceNode.ResourceType extractsType;
    public int extractRate = 20; // par seconde

    [Header("UI Production")]
    public Canvas productionCanvas;
    public Slider productionBar;
    public Text productionText;

    [Header("Configuration Bâtiment")]
    public string buildingName = "Quartier Général";
    public int maxHealth = 1000;
    public int currentHealth;

    [Header("Production d'Unités")]
    public UnitType[] availableUnits = new UnitType[0]; // Array vide par défaut
    public Transform spawnPoint;

    [Header("états")]
    public bool isSelected = false;
    public bool isProducing = false;
    private float productionTimer = 0f;

    [Header("Visuel")]
    private Renderer buildingRenderer;
    public Material selectedMaterial;
    private Material originalMaterial;

    [Header("Queue de Production")]
    public Queue<string> productionQueue = new Queue<string>();
    public int maxQueueSize = 5;

    public void Start()
    {
        currentHealth = maxHealth;
        buildingRenderer = GetComponent<Renderer>();
        originalMaterial = buildingRenderer.material;

        // Créer le point de spawn s'il n'existe pas
        if (spawnPoint == null)
        {
            CreateSpawnPoint();
        }

        Debug.Log($"{buildingName} initialisé. SpawnPoint: {spawnPoint != null}");
        CreateProductionUI();
    }

    void Update()
    {
        HandleProduction();
        UpdateProductionUI();
    }

    void HandleProduction()
    {
        if (isProducing && productionQueue.Count > 0)
        {
            string currentUnitType = productionQueue.Peek();
            UnitType unitType = System.Array.Find(availableUnits, u => u.name == currentUnitType);

            if (unitType == null) return;

            productionTimer += Time.deltaTime;

            if (productionTimer >= unitType.productionTime)
            {
                Debug.Log($"Production terminée ! Timer: {productionTimer}/{unitType.productionTime}");
                ProduceUnit();

                productionTimer = 0f;
                productionQueue.Dequeue();

                if (productionQueue.Count == 0)
                {
                    isProducing = false;
                    Debug.Log("Production arrétée - Queue vide");
                }
            }
        }
    }


    void CreateSpawnPoint()
    {
        GameObject spawnGO = new GameObject("SpawnPoint");
        spawnGO.transform.SetParent(transform);

        // Position devant le bétiment sur la NavMesh
        Vector3 frontPosition = transform.position + (transform.forward * -5f);

        // Vérifier que le point est sur la NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(frontPosition, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnGO.transform.position = hit.position;
        }
        else
        {
            // Fallback: utiliser la position du bétiment
            spawnGO.transform.position = transform.position;
        }

        spawnPoint = spawnGO.transform;
        Debug.Log($"SpawnPoint créé é: {spawnPoint.position}");
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selected && selectedMaterial != null)
        {
            buildingRenderer.material = selectedMaterial;
        }
        else
        {
            buildingRenderer.material = originalMaterial;
        }

        Debug.Log($"{buildingName} {(selected ? "sélectionné" : "désélectionné")}");
    }

    public void AddToProductionQueue(string unitTypeName)
    {
        if (isExtractor) return;

        // Trouver le type d'unité
        UnitType unitType = System.Array.Find(availableUnits, u => u.name == unitTypeName);
        if (unitType == null)
        {
            Debug.LogError($"Type d'unité '{unitTypeName}' introuvable !");
            return;
        }

        // Vérifier les ressources
        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(unitType.cost, 0, 1))
        {
            Debug.Log($"Ressources insuffisantes pour produire {unitTypeName}! Coét: {unitType.cost}");
            return;
        }

        if (productionQueue.Count < maxQueueSize)
        {
            // Dépenser les ressources
            bool resourcesSpent = true;
            if (ResourceManager.Instance != null)
            {
                resourcesSpent = ResourceManager.Instance.SpendResources(unitType.cost, 0, 1);
            }

            if (resourcesSpent)
            {
                productionQueue.Enqueue(unitTypeName);
                Debug.Log($"Ajouté {unitTypeName} é la queue. Coét: {unitType.cost} crédits. Queue: {productionQueue.Count}/{maxQueueSize}");

                if (!isProducing)
                {
                    isProducing = true;
                    productionTimer = 0f;
                    Debug.Log("Production démarrée");
                }
            }
        }
        else
        {
            Debug.Log("Queue de production pleine !");
        }
    }


    void ProduceUnit()
    {
        if (isExtractor || productionQueue.Count == 0) return;

        string unitTypeName = productionQueue.Peek();
        UnitType unitType = System.Array.Find(availableUnits, u => u.name == unitTypeName);

        if (unitType == null || unitType.prefab == null)
        {
            Debug.LogError($"Prefab manquant pour {unitTypeName}!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("spawnPoint manquant !");
            return;
        }

        // Position de spawn sécurisée
        Vector3 spawnPosition = spawnPoint.position;

        // Vérifier que la position est sur la NavMesh
        UnityEngine.AI.NavMeshHit navHit;
        if (!UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            Debug.LogWarning("SpawnPoint pas sur NavMesh, utilisation position bétiment");
            spawnPosition = transform.position;
        }
        else
        {
            spawnPosition = navHit.position;
        }

        // Instancier l'unité
        GameObject newUnit = Instantiate(unitType.prefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Unité créée: {newUnit.name} ({unitTypeName}) é la position {spawnPosition}");

        // Déplacer légérement aprés spawn
        StartCoroutine(MoveUnitAfterSpawn(newUnit));
    }


    System.Collections.IEnumerator MoveUnitAfterSpawn(GameObject unit)
    {
        // Attendre une frame pour que le NavMeshAgent s'initialise
        yield return null;

        UnitMovement unitMovement = unit.GetComponent<UnitMovement>();
        if (unitMovement != null)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-3f, 3f),
                0,
                Random.Range(-3f, 3f)
            );

            Vector3 targetPosition = spawnPoint.position + randomOffset;

            // Vérifier que la destination est sur la NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                unitMovement.MoveTo(hit.position);
            }
        }
    }

    public float GetProductionProgress()
    {
        if (isProducing && productionQueue.Count > 0)
        {
            string currentUnitType = productionQueue.Peek();
            UnitType unitType = System.Array.Find(availableUnits, u => u.name == currentUnitType);

            if (unitType != null && unitType.productionTime > 0)
            {
                return productionTimer / unitType.productionTime;
            }
        }
        return 0f;
    }


    public bool CanProduce()
    {
        return productionQueue.Count < maxQueueSize;
    }

    void CreateProductionUI()
    {
        // Créer Canvas UI World Space
        GameObject canvasGO = new GameObject("ProductionCanvas");
        canvasGO.transform.SetParent(transform);

        productionCanvas = canvasGO.AddComponent<Canvas>();
        productionCanvas.renderMode = RenderMode.WorldSpace;
        productionCanvas.worldCamera = Camera.main;
        productionCanvas.sortingOrder = 10;

        // Position fixe au-dessus
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3, 0.5f);
        canvasRect.localPosition = new Vector3(0, 4f, 0);

        // Slider UI classique
        GameObject sliderGO = new GameObject("ProductionSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);

        productionBar = sliderGO.AddComponent<Slider>();
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(3, 0.3f);
        sliderRect.anchoredPosition = Vector2.zero;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Opaque

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;

        // Fill Area + Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Vert opaque

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;

        // Configuration Slider
        productionBar.fillRect = fillRect;
        productionBar.minValue = 0;
        productionBar.maxValue = 1;
        productionBar.value = 0;

        productionCanvas.gameObject.SetActive(false);

        Debug.Log("UI Production Canvas créée");
    }

    void UpdateProductionUI()
    {
        if (productionCanvas == null || productionBar == null) return;

        bool shouldShow = isProducing && productionQueue.Count > 0;
        productionCanvas.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            float progress = GetProductionProgress();
            productionBar.value = progress;

            Debug.Log($"Production UI: {progress * 100:F0}%");
        }
    }


    [Header("UI Production Simple")]
    private Transform productionBg;
    private Transform productionFill;



}
