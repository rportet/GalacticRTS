using UnityEngine;

[System.Serializable]
public class BuildingType
{
    public string name;
    public GameObject prefab;
    public int creditsCost;
    public int energieCost;
    public float constructionTime;
    public KeyCode shortcut;
    public string description;
}

public class BuildingConstruction : MonoBehaviour
{
    [Header("Construction Différée")]
    private Vector3 pendingBuildPosition;
    private bool hasPendingBuild = false;

    [Header("Types de Bâtiments")]
    public BuildingType[] availableBuildings = new BuildingType[0];

    [Header("Construction Avancée")]
    public float buildRange = 5f;
    public LayerMask groundLayer = 1 << 6;
    public Material previewMaterial;

    private GameObject currentPreview;
    private BuildingType selectedBuildingType;
    private bool isInBuildMode = false;
    private Camera mainCamera;

    public static BuildingConstruction Instance;

    void Awake()
    {
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
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (isInBuildMode)
        {
            HandleBuildingPreview();
            HandleBuildingPlacement();
        }

        // Vérifier construction en attente
        if (hasPendingBuild)
        {
            CheckPendingConstruction();
        }
    }

    void CheckPendingConstruction()
    {
        // Vérifier qu'on a encore un worker sélectionné
        if (RTSController.Instance == null || RTSController.Instance.selectedUnits.Count == 0)
        {
            Debug.Log("Plus de worker sélectionné, annulation construction en attente");
            CancelBuildMode();
            return;
        }

        // Vérifier qu'il y a bien un worker dans la sélection
        bool hasWorker = false;
        foreach (UnitMovement unit in RTSController.Instance.selectedUnits)
        {
            if (unit.GetComponent<Worker>() != null)
            {
                hasWorker = true;
                break;
            }
        }

        if (!hasWorker)
        {
            Debug.Log("Plus de worker sélectionné, annulation construction");
            CancelBuildMode();
            return;
        }

        // Vérifier si worker en portée
        if (!IsWorkerInRange(pendingBuildPosition))
        {
            return; // Worker pas encore arrivé
        }

        Debug.Log("Worker arrivé ! Construction automatique...");

        // Vérifier position
        if (!IsPositionValid(pendingBuildPosition))
        {
            Debug.Log("Position plus valide, annulation construction");
            CancelPendingBuild();
            return;
        }

        // Construire !
        if (ResourceManager.Instance.SpendResources(selectedBuildingType.creditsCost, selectedBuildingType.energieCost, 0))
        {
            GameObject newBuilding = Instantiate(selectedBuildingType.prefab, pendingBuildPosition, Quaternion.identity);
            Debug.Log($"✅ Bâtiment construit : {selectedBuildingType.name}");

            PushUnitsAway(pendingBuildPosition, 3f);
            CancelBuildMode();
        }
        else
        {
            Debug.Log("Plus assez de ressources !");
            CancelPendingBuild();
        }
    }

    void CancelPendingBuild()
    {
        hasPendingBuild = false;
        pendingBuildPosition = Vector3.zero;
        Debug.Log("Construction en attente annulée");
    }

    public void StartBuildMode(string buildingName)
    {
        selectedBuildingType = System.Array.Find(availableBuildings, b => b.name == buildingName);
        if (selectedBuildingType == null)
        {
            Debug.LogError($"Type de bâtiment '{buildingName}' introuvable !");
            return;
        }

        // Vérifier les ressources
        if (!ResourceManager.Instance.CanAfford(selectedBuildingType.creditsCost, selectedBuildingType.energieCost, 0))
        {
            Debug.Log($"Ressources insuffisantes pour {buildingName}!");
            return;
        }

        isInBuildMode = true;
        CreatePreview();
        Debug.Log($"Mode construction activé : {buildingName}");
    }

    void CreatePreview()
{
    if (selectedBuildingType == null || selectedBuildingType.prefab == null) return;
    
    // Détruire preview précédent
    if (currentPreview != null)
    {
        Destroy(currentPreview);
    }
    
    // Créer preview
    currentPreview = Instantiate(selectedBuildingType.prefab);
    
    // *** SUPPRIMER TOUS LES SCRIPTS BUILDING ! ***
    Building buildingScript = currentPreview.GetComponent<Building>();
    if (buildingScript != null)
    {
        DestroyImmediate(buildingScript);
        Debug.Log("Script Building supprimé du preview");
    }
    
    // Supprimer aussi ExtractorBehavior, Barracks, etc.
    MonoBehaviour[] allScripts = currentPreview.GetComponents<MonoBehaviour>();
    foreach (var script in allScripts)
    {
        if (script != null)
        {
            DestroyImmediate(script);
        }
    }
    
    // Matériau transparent vert
    Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
    foreach (var renderer in renderers)
    {
        Material transparentMat = new Material(Shader.Find("Standard"));
        transparentMat.color = new Color(0, 1, 0, 0.5f);
        transparentMat.SetFloat("_Mode", 3);
        transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        transparentMat.SetInt("_ZWrite", 0);
        transparentMat.DisableKeyword("_ALPHATEST_ON");
        transparentMat.EnableKeyword("_ALPHABLEND_ON");
        transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        transparentMat.renderQueue = 3000;
        renderer.material = transparentMat;
    }
    
    // Désactiver colliders
    Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
    foreach (var collider in colliders)
    {
        collider.enabled = false;
    }
    
    Debug.Log("Preview créé sans scripts Building");
}


    void HandleBuildingPreview()
    {
        if (currentPreview == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            currentPreview.transform.position = hit.point;
        }
    }

    void HandleBuildingPlacement()
    {
        if (Input.GetMouseButtonDown(0)) // Clic gauche - Construire
        {
            PlaceBuilding();
        }

        if (Input.GetMouseButtonDown(1)) // Clic droit - Annuler
        {
            CancelBuildMode();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) // Échap pour annuler aussi
        {
            CancelBuildMode();
        }
    }

    void PlaceBuilding()
    {
        if (currentPreview == null || selectedBuildingType == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Debug.Log("Position invalide pour construire !");
            return;
        }

        Vector3 buildPos = hit.point;

        // Vérifier la portée du worker
        if (!IsWorkerInRange(buildPos))
        {
            Debug.Log("Worker trop loin, déplacement + construction différée");

            pendingBuildPosition = buildPos;
            hasPendingBuild = true;

            // Déplacer le worker
            if (RTSController.Instance != null)
            {
                foreach (UnitMovement unit in RTSController.Instance.selectedUnits)
                {
                    Worker worker = unit.GetComponent<Worker>();
                    if (worker != null)
                    {
                        Vector3 workerTargetPos = buildPos + Vector3.back * 2f;
                        worker.OrderMoveTo(workerTargetPos);
                        Debug.Log("Worker envoyé + construction programmée");
                        break;
                    }
                }
            }
            return;
        }

        // Worker assez proche, construction immédiate
        if (!IsPositionValid(buildPos))
        {
            Debug.Log("Position occupée ! Impossible de construire ici.");
            return;
        }

        // Construction normale
        if (!ResourceManager.Instance.SpendResources(selectedBuildingType.creditsCost, selectedBuildingType.energieCost, 0))
        {
            Debug.Log("Ressources insuffisantes !");
            return;
        }

        GameObject newBuilding = Instantiate(selectedBuildingType.prefab, buildPos, Quaternion.identity);
        Debug.Log($"✅ Bâtiment construit immédiatement : {selectedBuildingType.name}");

        PushUnitsAway(buildPos, 3f);
        CancelBuildMode();
    }

    bool IsPositionValid(Vector3 position)
    {
        Debug.Log($"=== Vérification position: {position} ===");

        float minBuildingDistance = 5f; // RÉDUIT de 8 à 5

        Building[] buildings = FindObjectsOfType<Building>();
        Debug.Log($"Bâtiments trouvés: {buildings.Length}");

        foreach (Building building in buildings)
        {
            if (building == null)
            {
                Debug.LogWarning("Bâtiment null trouvé !");
                continue;
            }

            Vector3 buildingPos = building.transform.position;
            float realDistance = Vector3.Distance(position, buildingPos);

            Debug.Log($"--- {building.buildingName} ---");
            Debug.Log($"Distance calculée: {realDistance:F2}m (min: {minBuildingDistance}m)");

            if (realDistance < minBuildingDistance)
            {
                Debug.Log($"❌ REJETÉ - Trop proche de {building.buildingName}");
                return false;
            }
            else
            {
                Debug.Log($"✅ OK pour {building.buildingName}");
            }
        }

        Debug.Log("✅ Position finale VALIDE");
        return true;
    }

    bool IsWorkerInRange(Vector3 buildPosition)
    {
        if (RTSController.Instance == null)
        {
            Debug.LogError("RTSController.Instance est null !");
            return false;
        }

        if (RTSController.Instance.selectedUnits.Count == 0)
        {
            Debug.Log("Aucune unité sélectionnée");
            return false;
        }

        foreach (UnitMovement unit in RTSController.Instance.selectedUnits)
        {
            Worker worker = unit.GetComponent<Worker>();
            if (worker != null)
            {
                float distance = Vector3.Distance(worker.transform.position, buildPosition);
                Debug.Log($"Worker {worker.name} - Distance: {distance:F1}m / Portée: {buildRange}m");

                bool inRange = distance <= (buildRange + 2f);
                Debug.Log($"Worker en portée: {(inRange ? "✅ OUI" : "❌ NON")}");
                return inRange;
            }
        }

        Debug.Log("Aucun worker trouvé dans la sélection");
        return false;
    }

    void PushUnitsAway(Vector3 buildPosition, float radius)
    {
        UnitMovement[] units = FindObjectsOfType<UnitMovement>();
        foreach (UnitMovement unit in units)
        {
            float distance = Vector3.Distance(buildPosition, unit.transform.position);
            if (distance < radius)
            {
                Vector3 direction = (unit.transform.position - buildPosition).normalized;
                Vector3 newPosition = buildPosition + direction * (radius + 1f);

                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(newPosition, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    unit.MoveTo(navHit.position);
                    Debug.Log($"Unité {unit.gameObject.name} poussée vers {navHit.position}");
                }
            }
        }
    }

    void CancelBuildMode()
    {
        isInBuildMode = false;
        selectedBuildingType = null;
        hasPendingBuild = false;

        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        Debug.Log("Mode construction annulé");
    }

    public bool IsInBuildMode()
    {
        return isInBuildMode;
    }
}
