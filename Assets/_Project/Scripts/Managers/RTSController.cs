using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RTSController : MonoBehaviour
{
    public static RTSController Instance; // SINGLETON

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("RTSController Instance créée");
        }
        else
        {
            Debug.LogWarning("RTSController en double détecté !");
            Destroy(gameObject);
        }
    }
    [Header("UI Tooltips")]
    public Text tooltipText;

    [Header("Configuration")]
    public LayerMask unitLayer = 1 << 7;
    public LayerMask groundLayer = 1 << 6;

    [Header("Sélection")]
    public List<UnitMovement> selectedUnits = new List<UnitMovement>();
    public Material selectedMaterial;

    [Header("Bétiments")]
    public List<Building> selectedBuildings = new List<Building>();
    public LayerMask buildingLayer = 1 << 7; // Méme layer pour l'instant

    [Header("Selection Box")]
    public RectTransform selectionBox;
    private Vector2 startPosition;
    private Camera mainCamera;
    private bool isDragging = false;

    [Header("Double Clic")]
    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.5f;
    private UnitMovement lastClickedUnit = null;

    [Header("Double Clic Droit")]
    private float lastRightClickTime = 0f;


    void Start()
    {
        mainCamera = Camera.main;
        CreateSelectionBox();
        CreateTooltips();
    }

    void Update()
    {
        HandleInput();
        HandleKeyboardShortcuts();
        UpdateTooltips();
    }

    void HandleKeyboardShortcuts()
    {
        // Construction de bâtiments (Worker seulement)
        if (GetKeyDown(KeyCode.B))
        {
            HandleWorkerConstruction("Caserne");
        }

        if (GetKeyDown(KeyCode.E))
        {
            HandleWorkerConstruction("Extracteur");
        }

        // Production d'unités selon les raccourcis définis
        foreach (Building building in selectedBuildings)
        {
            if (building.availableUnits != null)
            {
                foreach (var unitType in building.availableUnits)
                {
                    if (GetKeyDown(unitType.shortcut))
                    {
                        Debug.Log($"Tentative de production: {unitType.name}");
                        building.AddToProductionQueue(unitType.name);
                    }
                }
            }
        }
    }

    void HandleWorkerConstruction(string buildingName)
    {
        // Vérifier qu'on a un worker sélectionné
        Worker selectedWorker = null;
        foreach (UnitMovement unit in selectedUnits)
        {
            Worker worker = unit.GetComponent<Worker>();
            if (worker != null)
            {
                selectedWorker = worker;
                break;
            }
        }

        if (selectedWorker != null)
        {
            Debug.Log($"Worker trouvé, construction {buildingName} activée !");
            if (BuildingConstruction.Instance != null)
            {
                BuildingConstruction.Instance.StartBuildMode(buildingName);
            }
        }
        else
        {
            Debug.Log($"Sélectionnez un Worker pour construire {buildingName} !");
        }
    }




    bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        // Utilise le nom de la touche : key.ToString()
        var keyName = key.ToString().ToUpper();

        // Pour les lettres A-Z
        if (keyName.Length == 1 && char.IsLetter(keyName[0]))
        {
            var inputKey = (Key)System.Enum.Parse(typeof(Key), keyName);
            return Keyboard.current[inputKey].wasPressedThisFrame;
        }

        // Pour les touches spéciales
        switch (key)
        {
            case KeyCode.LeftControl: return Keyboard.current[Key.LeftCtrl].wasPressedThisFrame;
            case KeyCode.RightControl: return Keyboard.current[Key.RightCtrl].wasPressedThisFrame;
            case KeyCode.Escape: return Keyboard.current[Key.Escape].wasPressedThisFrame;
            case KeyCode.Alpha1: return Keyboard.current[Key.Digit1].wasPressedThisFrame;
            case KeyCode.Alpha2: return Keyboard.current[Key.Digit2].wasPressedThisFrame;
            case KeyCode.Alpha3: return Keyboard.current[Key.Digit3].wasPressedThisFrame;
        }
        return false;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#endif
        return false;
    }


    // Méthode helper pour convertir KeyCode en Key
    Key KeyCodeToKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.A: return Key.A;
            case KeyCode.B: return Key.B;
            case KeyCode.C: return Key.C;
            case KeyCode.D: return Key.D;
            case KeyCode.E: return Key.E;
            case KeyCode.F: return Key.F;
            case KeyCode.G: return Key.G;
            case KeyCode.H: return Key.H;
            case KeyCode.I: return Key.I;
            case KeyCode.J: return Key.J;
            case KeyCode.K: return Key.K;
            case KeyCode.L: return Key.L;
            case KeyCode.M: return Key.M;
            case KeyCode.N: return Key.N;
            case KeyCode.O: return Key.O;
            case KeyCode.P: return Key.P;
            case KeyCode.Q: return Key.Q;
            case KeyCode.R: return Key.R;
            case KeyCode.S: return Key.S;
            case KeyCode.T: return Key.T;
            case KeyCode.U: return Key.U;
            case KeyCode.V: return Key.V;
            case KeyCode.W: return Key.W;
            case KeyCode.X: return Key.X;
            case KeyCode.Y: return Key.Y;
            case KeyCode.Z: return Key.Z;
            case KeyCode.LeftControl: return Key.LeftCtrl;
            case KeyCode.RightControl: return Key.RightCtrl;
            default: return Key.None;
        }
    }




    void HandleInput()
    {
        bool leftClick = GetMouseButtonDown(0);
        bool leftHold = GetMouseButton(0);
        bool leftRelease = GetMouseButtonUp(0);
        bool rightClick = GetMouseButtonDown(1);

        if (leftClick) StartSelection();
        if (leftHold) UpdateSelectionBox();
        if (leftRelease) EndSelection();
        if (rightClick) MoveSelectedUnits();
    }

    // Méthodes compatibles avec les deux systémes
    bool GetMouseButtonDown(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return button == 0 ? Mouse.current.leftButton.wasPressedThisFrame :
                   button == 1 ? Mouse.current.rightButton.wasPressedThisFrame : false;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(button);
#endif
        return false;
    }

    bool GetMouseButton(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return button == 0 ? Mouse.current.leftButton.isPressed : false;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(button);
#endif
        return false;
    }

    bool GetMouseButtonUp(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return button == 0 ? Mouse.current.leftButton.wasReleasedThisFrame : false;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonUp(button);
#endif
        return false;
    }

    bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current[Key.LeftCtrl].isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(key);
#endif
        return false;
    }

    Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#endif
        return Vector3.zero;
    }

    void StartSelection()
    {
        startPosition = GetMousePosition();
        Debug.Log($"Début sélection à : {startPosition}");

        selectionBox.gameObject.SetActive(true);
        selectionBox.sizeDelta = Vector2.zero; // Reset taille
        selectionBox.position = startPosition; // Position initiale

        isDragging = true;

        if (!GetKey(KeyCode.LeftControl))
        {
            DeselectAllUnits();
            DeselectAllBuildings();
        }
    }


    void UpdateSelectionBox()
    {
        if (!isDragging) return;

        Vector2 currentMousePos = GetMousePosition();

        // Calculer les coins du rectangle
        Vector2 min = new Vector2(
            Mathf.Min(startPosition.x, currentMousePos.x),
            Mathf.Min(startPosition.y, currentMousePos.y)
        );

        Vector2 max = new Vector2(
            Mathf.Max(startPosition.x, currentMousePos.x),
            Mathf.Max(startPosition.y, currentMousePos.y)
        );

        // Position = coin bas-gauche, Taille = différence
        selectionBox.anchoredPosition = min;
        selectionBox.sizeDelta = max - min;

        Debug.Log($"Rectangle - Min: {min}, Max: {max}, Size: {max - min}");
    }



    void EndSelection()
    {
        Debug.Log($"Fin sélection. Distance parcourue: {Vector2.Distance(startPosition, GetMousePosition())}");

        selectionBox.gameObject.SetActive(false);
        isDragging = false;

        Vector2 currentMousePos = GetMousePosition();

        // Sélection par box ou par clic
        if (Vector2.Distance(startPosition, currentMousePos) < 10f)
        {
            Debug.Log("Clic simple détecté");
            SelectSingleUnit();
        }
        else
        {
            Debug.Log("Sélection par drag détectée");
            SelectUnitsInBox();
        }
    }

    void SelectSingleUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(GetMousePosition());
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, unitLayer))
        {
            UnitMovement unit = hit.collider.GetComponent<UnitMovement>();
            if (unit != null)
            {
                float currentTime = Time.time;
                bool isDoubleClick = (currentTime - lastClickTime) < doubleClickThreshold && lastClickedUnit == unit;

                if (isDoubleClick)
                {
                    // Double clic - sélectionner toutes les unités du même type visibles
                    SelectAllVisibleUnitsOfType(unit);
                }
                else
                {
                    // Clic simple normal
                    if (!GetKey(KeyCode.LeftControl))
                    {
                        DeselectAllUnits();
                        DeselectAllBuildings();
                    }
                    SelectUnit(unit);
                }

                lastClickTime = currentTime;
                lastClickedUnit = unit;
                return;
            }

            // Vérifier bâtiments (code existant...)
            Building building = hit.collider.GetComponent<Building>();
            if (building != null)
            {
                if (!GetKey(KeyCode.LeftControl))
                {
                    DeselectAllUnits();
                    DeselectAllBuildings();
                }
                SelectBuilding(building);
                return;
            }

            // NOUVEAU: Vérifier extracteur
            BuildingSelection extractorSelection = hit.collider.GetComponent<BuildingSelection>();
            if (extractorSelection != null)
            {
                if (!GetKey(KeyCode.LeftControl))
                {
                    DeselectAllUnits();
                    DeselectAllBuildings();
                    DeselectAllExtractors(); // Nouvelle méthode
                }

                SelectExtractor(extractorSelection);
                return;
            }
        }
    }

    void SelectExtractor(BuildingSelection extractor)
    {
        extractor.SetSelected(true, selectedMaterial);
        Debug.Log($"Extracteur sélectionné : {extractor.gameObject.name}");
    }

    void DeselectAllExtractors()
    {
        BuildingSelection[] extractors = FindObjectsOfType<BuildingSelection>();
        foreach (BuildingSelection extractor in extractors)
        {
            extractor.SetSelected(false);
        }
    }

    void SelectAllVisibleUnitsOfType(UnitMovement referenceUnit)
    {
        DeselectAllUnits();
        DeselectAllBuildings();

        // Déterminer le type de l'unité
        System.Type unitType = referenceUnit.GetType();
        string unitTypeName = unitType.Name;

        Debug.Log($"Double-clic détecté ! Sélection de toutes les unités {unitTypeName}");

        UnitMovement[] allUnits = FindObjectsOfType<UnitMovement>();
        int selectedCount = 0;

        foreach (UnitMovement unit in allUnits)
        {
            // Vérifier si même type
            if (unit.GetType() == unitType)
            {
                // Vérifier si visible à l'écran
                Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    SelectUnit(unit);
                    selectedCount++;
                }
            }
        }

        Debug.Log($"{selectedCount} unités {unitTypeName} sélectionnées");
    }


    void SelectBuilding(Building building)
    {
        if (!selectedBuildings.Contains(building))
        {
            selectedBuildings.Add(building);
            building.SetSelected(true);
        }
    }

    void DeselectAllBuildings()
    {
        foreach (Building building in selectedBuildings)
        {
            building.SetSelected(false);
        }
        selectedBuildings.Clear();
    }


    void SelectUnitsInBox()
    {
        Vector2 min = new Vector2(
            Mathf.Min(startPosition.x, GetMousePosition().x),
            Mathf.Min(startPosition.y, GetMousePosition().y)
        );
        Vector2 max = new Vector2(
            Mathf.Max(startPosition.x, GetMousePosition().x),
            Mathf.Max(startPosition.y, GetMousePosition().y)
        );

        UnitMovement[] allUnits = FindObjectsOfType<UnitMovement>();
        foreach (UnitMovement unit in allUnits)
        {
            Vector2 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
            if (screenPos.x >= min.x && screenPos.x <= max.x &&
                screenPos.y >= min.y && screenPos.y <= max.y)
            {
                SelectUnit(unit);
            }
        }
    }

    void SelectUnit(UnitMovement unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true, selectedMaterial);
        }
    }

    void DeselectAllUnits()
    {
        foreach (UnitMovement unit in selectedUnits)
        {
            unit.SetSelected(false, null);
        }
        selectedUnits.Clear();
    }

    void MoveSelectedUnits()
    {
        // Détecter double clic droit
        float currentTime = Time.time;
        bool isDoubleRightClick = (currentTime - lastRightClickTime) < doubleClickThreshold;
        lastRightClickTime = currentTime;

        Ray ray = mainCamera.ScreenPointToRay(GetMousePosition());
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Vérifier TOUTE cible attaquable
            bool canAttack = false;

            // Bâtiments
            if (hit.collider.GetComponent<Building>() != null) canAttack = true;

            // Extracteurs  
            if (hit.collider.GetComponent<BuildingSelection>() != null) canAttack = true;

            // Unités ennemies (toute UnitMovement différente de nos unités)
            UnitMovement targetUnit = hit.collider.GetComponent<UnitMovement>();
            if (targetUnit != null && !selectedUnits.Contains(targetUnit)) canAttack = true;

            if (canAttack)
            {
                // Commande d'attaque pour tous les soldats
                foreach (UnitMovement unit in selectedUnits)
                {
                    Soldier soldier = unit.GetComponent<Soldier>();
                    if (soldier != null)
                    {
                        soldier.AttackTarget(hit.collider.gameObject);
                    }
                }
                return;
            }

            // Sinon, déplacement normal - UTILISER OrderMoveTo pour nos unités spéciales
            if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
            {
                // Si double clic droit, activer la course
                if (isDoubleRightClick)
                {
                    Debug.Log("Double clic droit - Mode course activé !");
                    foreach (UnitMovement unit in selectedUnits)
                    {
                        unit.SetRunning(true);
                    }
                }

                // Formation en carré pour plusieurs unités
                if (selectedUnits.Count > 1)
                {
                    int unitsPerRow = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
                    float spacing = 2f;

                    for (int i = 0; i < selectedUnits.Count; i++)
                    {
                        int row = i / unitsPerRow;
                        int col = i % unitsPerRow;

                        Vector3 offset = new Vector3(
                            (col - unitsPerRow * 0.5f) * spacing,
                            0,
                            (row - unitsPerRow * 0.5f) * spacing
                        );

                        Vector3 targetPos = hit.point + offset;

                        // Utiliser OrderMoveTo pour les unités spéciales
                        Worker worker = selectedUnits[i].GetComponent<Worker>();
                        if (worker != null)
                        {
                            worker.OrderMoveTo(targetPos);
                        }
                        else
                        {
                            Soldier soldier = selectedUnits[i].GetComponent<Soldier>();
                            if (soldier != null)
                            {
                                soldier.OrderMoveTo(targetPos);
                            }
                            else
                            {
                                selectedUnits[i].MoveTo(targetPos);
                            }
                        }
                    }
                }
                else if (selectedUnits.Count == 1)
                {
                    Vector3 targetPos = hit.point;

                    Worker worker = selectedUnits[0].GetComponent<Worker>();
                    if (worker != null)
                    {
                        worker.OrderMoveTo(targetPos);
                    }
                    else
                    {
                        Soldier soldier = selectedUnits[0].GetComponent<Soldier>();
                        if (soldier != null)
                        {
                            soldier.OrderMoveTo(targetPos);
                        }
                        else
                        {
                            selectedUnits[0].MoveTo(targetPos);
                        }
                    }
                }
            }
        }
    }



    void CreateSelectionBox()
    {
        // Utiliser le Canvas EXISTANT
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Aucun Canvas trouvé pour la SelectionBox!");
            return;
        }

        // Créer Selection Box
        GameObject selectionBoxGO = new GameObject("Selection Box");
        selectionBoxGO.transform.SetParent(canvas.transform, false);

        selectionBox = selectionBoxGO.AddComponent<RectTransform>();
        UnityEngine.UI.Image image = selectionBoxGO.AddComponent<UnityEngine.UI.Image>();

        // Style de la box
        image.color = new Color(0, 1, 0, 0.2f);
        selectionBoxGO.SetActive(false);

        // CONFIGURATION CORRECTE DU RECTTRANSFORM
        selectionBox.anchorMin = new Vector2(0, 0);
        selectionBox.anchorMax = new Vector2(0, 0);
        selectionBox.pivot = new Vector2(0, 0);
        selectionBox.anchoredPosition = Vector2.zero;
        selectionBox.sizeDelta = Vector2.zero;
    }

    void CreateTooltips()
    {
        // COPIER EXACTEMENT la logique de ResourceManager
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

        // Vérifier si tooltip existe déjà
        Transform existingTooltip = canvas.transform.Find("TooltipPanel");
        if (existingTooltip != null)
        {
            Debug.Log("TooltipPanel existe déjà, on le récupère");
            tooltipText = existingTooltip.GetComponentInChildren<Text>();
            return;
        }

        // Créer tooltip panel EXACTEMENT comme ResourcePanel
        GameObject tooltipPanelGO = new GameObject("TooltipPanel");
        tooltipPanelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = tooltipPanelGO.AddComponent<RectTransform>();
        Image panelImage = tooltipPanelGO.AddComponent<Image>();

        // Position en bas à gauche - MÊME LOGIQUE que ResourcePanel
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(250, 80);

        panelImage.color = new Color(0, 0, 0, 0.7f);

        // Layout vertical COMME ResourcePanel
        VerticalLayoutGroup layoutGroup = tooltipPanelGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 5;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;

        // Créer le texte EXACTEMENT comme dans ResourceManager
        tooltipText = CreateTooltipText("", tooltipPanelGO);

        // Masquer par défaut
        tooltipPanelGO.SetActive(false);

        Debug.Log("Tooltips créés avec succès !");
    }

    // COPIER EXACTEMENT CreateResourceText mais pour tooltip
    Text CreateTooltipText(string initialText, GameObject parent)
    {
        GameObject textGO = new GameObject("TooltipText");
        textGO.transform.SetParent(parent.transform);
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = initialText;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        return textComponent;
    }


    void UpdateTooltips()
    {
        if (tooltipText == null || tooltipText.transform.parent == null) return;

        GameObject tooltipPanel = tooltipText.transform.parent.gameObject;

        if (selectedBuildings.Count > 0)
        {
            tooltipText.text = "Q - Worker (50 crédits)\nS - Soldier (100 crédits)\nD - Heavy (150 crédits)";
            tooltipPanel.SetActive(true);
        }
        else if (selectedUnits.Count > 0)
        {
            // Vérifier si worker sélectionné
            bool hasWorker = false;
            foreach (UnitMovement unit in selectedUnits)
            {
                if (unit.GetComponent<Worker>() != null)
                {
                    hasWorker = true;
                    break;
                }
            }

            if (hasWorker)
            {
                tooltipText.text = "B - Construire Caserne (200 crédits)\nE - Construire Extracteur";
            }
            else
            {
                tooltipText.text = "Clic droit - Attaquer cible\nClic gauche - Déplacer";
            }
            tooltipPanel.SetActive(true);
        }
        else
        {
            tooltipPanel.SetActive(false);
        }
    }

}
