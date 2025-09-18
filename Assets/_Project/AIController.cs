using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("IA Configuration")]
    public FactionType aiFaction = FactionType.AI_Enemy;
    public float thinkInterval = 2f; // IA réfléchit toutes les 2 secondes
    public int maxWorkers = 3;
    public int maxSoldiers = 10;
    
    [Header("Références IA")]
    public Building aiHeadquarters;
    public List<Building> aiBuildings = new List<Building>();
    public List<UnitMovement> aiUnits = new List<UnitMovement>();
    
    private float lastThinkTime;
    private AIState currentState = AIState.Develop;
    
    enum AIState
    {
        Develop,    // Construction et économie
        Attack,     // Attaque offensive
        Defend      // Défense de base
    }
    
    void Start()
    {
        // Trouver le QG IA
        if (aiHeadquarters == null)
        {
            FindAIHeadquarters();
        }
        
        // Commencer à penser
        InvokeRepeating(nameof(Think), 1f, thinkInterval);
        
        Debug.Log($"IA {aiFaction} initialisée");
    }
    
    void FindAIHeadquarters()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();
        foreach (Building building in allBuildings)
        {
            Faction faction = building.GetComponent<Faction>();
            if (faction != null && faction.factionType == aiFaction)
            {
                aiHeadquarters = building;
                aiBuildings.Add(building);
                Debug.Log($"QG IA trouvé : {building.buildingName}");
                break;
            }
        }
    }
    
    void Think()
    {
        if (aiHeadquarters == null || !aiHeadquarters.GetComponent<HealthSystem>().IsAlive())
        {
            Debug.Log("IA détruite !");
            return;
        }
        
        UpdateAIUnits();
        AnalyzeSituation();
        ExecuteStrategy();
    }
    
    void UpdateAIUnits()
    {
        // Nettoyer liste des unités mortes
        aiUnits.RemoveAll(unit => unit == null || !unit.GetComponent<HealthSystem>().IsAlive());
        
        // Trouver nouvelles unités IA
        UnitMovement[] allUnits = FindObjectsOfType<UnitMovement>();
        foreach (UnitMovement unit in allUnits)
        {
            Faction faction = unit.GetComponent<Faction>();
            if (faction != null && faction.factionType == aiFaction && !aiUnits.Contains(unit))
            {
                aiUnits.Add(unit);
            }
        }
        
        Debug.Log($"IA a {aiUnits.Count} unités");
    }
    
    void AnalyzeSituation()
    {
        int workers = CountUnitType<Worker>();
        int soldiers = CountUnitType<Soldier>();
        
        // Logique de décision simple
        if (workers < 2)
        {
            currentState = AIState.Develop;
        }
        else if (soldiers >= 3)
        {
            currentState = AIState.Attack;
        }
        else
        {
            currentState = AIState.Develop;
        }
        
        Debug.Log($"IA État: {currentState} - Workers: {workers}, Soldiers: {soldiers}");
    }
    
    void ExecuteStrategy()
    {
        switch (currentState)
        {
            case AIState.Develop:
                ExecuteDevelopment();
                break;
            case AIState.Attack:
                ExecuteAttack();
                break;
            case AIState.Defend:
                ExecuteDefense();
                break;
        }
    }
    
    void ExecuteDevelopment()
    {
        // 1. Produire des workers si pas assez
        int workers = CountUnitType<Worker>();
        if (workers < maxWorkers && CanAffordWorker())
        {
            ProduceUnit("Worker");
            Debug.Log("IA produit un Worker");
        }
        
        // 2. Construire extracteur avec premier worker
        if (workers > 0 && CountBuildings("Extracteur") == 0)
        {
            Worker firstWorker = GetFirstUnitOfType<Worker>();
            if (firstWorker != null)
            {
                BuildExtractor(firstWorker);
                Debug.Log("IA construit un Extracteur");
            }
        }
        
        // 3. Construire caserne
        if (workers >= 2 && CountBuildings("Caserne") == 0)
        {
            Worker builder = GetFirstUnitOfType<Worker>();
            if (builder != null)
            {
                BuildBarracks(builder);
                Debug.Log("IA construit une Caserne");
            }
        }
        
        // 4. Produire des soldiers
        if (CountBuildings("Caserne") > 0 && CountUnitType<Soldier>() < maxSoldiers)
        {
            ProduceUnit("Soldier");
            Debug.Log("IA produit un Soldier");
        }
    }
    
    void ExecuteAttack()
    {
        List<Soldier> soldiers = GetUnitsOfType<Soldier>();
        
        // Trouver cibles ennemies
        UnitMovement[] enemyUnits = FindEnemyUnits();
        Building[] enemyBuildings = FindEnemyBuildings();
        
        foreach (Soldier soldier in soldiers)
        {
            // Attaquer les unités d'abord, puis les bâtiments
            if (enemyUnits.Length > 0)
            {
                soldier.AttackTarget(enemyUnits[0].gameObject);
            }
            else if (enemyBuildings.Length > 0)
            {
                soldier.AttackTarget(enemyBuildings[0].gameObject);
            }
        }
    }
    
    void ExecuteDefense()
    {
        // Pour plus tard - logique défensive
    }
    
    #region Méthodes Utilitaires
    
    int CountUnitType<T>() where T : MonoBehaviour
    {
        int count = 0;
        foreach (UnitMovement unit in aiUnits)
        {
            if (unit.GetComponent<T>() != null) count++;
        }
        return count;
    }
    
    T GetFirstUnitOfType<T>() where T : MonoBehaviour
    {
        foreach (UnitMovement unit in aiUnits)
        {
            T component = unit.GetComponent<T>();
            if (component != null) return component;
        }
        return null;
    }
    
    List<T> GetUnitsOfType<T>() where T : MonoBehaviour
    {
        List<T> units = new List<T>();
        foreach (UnitMovement unit in aiUnits)
        {
            T component = unit.GetComponent<T>();
            if (component != null) units.Add(component);
        }
        return units;
    }
    
    int CountBuildings(string buildingName)
    {
        int count = 0;
        foreach (Building building in aiBuildings)
        {
            if (building.buildingName.Contains(buildingName)) count++;
        }
        return count;
    }
    
    bool CanAffordWorker()
    {
        // Supposer que ResourceManager gère l'IA aussi
        return ResourceManager.Instance.CanAfford(50, 0, 1);
    }
    
    void ProduceUnit(string unitType)
    {
        Building producer = null;
        
        if (unitType == "Worker" && aiHeadquarters != null)
        {
            producer = aiHeadquarters;
        }
        else if (unitType == "Soldier")
        {
            // Trouver caserne IA
            foreach (Building building in aiBuildings)
            {
                if (building.buildingName.Contains("Caserne"))
                {
                    producer = building;
                    break;
                }
            }
        }
        
        if (producer != null)
        {
            producer.AddToProductionQueue(unitType);
        }
    }
    
    void BuildExtractor(Worker worker)
    {
        // Trouver le ResourceNode le plus proche
        ResourceNode[] nodes = FindObjectsOfType<ResourceNode>();
        if (nodes.Length > 0)
        {
            ResourceNode nearestNode = nodes[0];
            Vector3 buildPos = nearestNode.transform.position;
            
            // Simuler construction d'extracteur
            StartCoroutine(DelayedBuild("Extracteur", buildPos, 2f));
        }
    }
    
    void BuildBarracks(Worker worker)
    {
        // Position près du QG
        Vector3 buildPos = aiHeadquarters.transform.position + Vector3.right * 8f;
        StartCoroutine(DelayedBuild("Caserne", buildPos, 3f));
    }
    
    IEnumerator DelayedBuild(string buildingType, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        GameObject prefab = null;
        
        // Trouver le bon prefab
        BuildingConstruction constructor = FindObjectOfType<BuildingConstruction>();
        if (constructor != null)
        {
            foreach (var buildingData in constructor.availableBuildings)
            {
                if (buildingData.name == buildingType)
                {
                    prefab = buildingData.prefab;
                    break;
                }
            }
        }
        
        if (prefab != null && ResourceManager.Instance.SpendResources(100, 25, 0))
        {
            GameObject newBuilding = Instantiate(prefab, position, Quaternion.identity);
            
            // Ajouter faction IA
            Faction faction = newBuilding.AddComponent<Faction>();
            faction.factionType = aiFaction;
            faction.factionColor = Color.red;
            
            // Ajouter à la liste
            Building buildingComponent = newBuilding.GetComponent<Building>();
            if (buildingComponent != null)
            {
                aiBuildings.Add(buildingComponent);
            }
            
            Debug.Log($"IA a construit: {buildingType}");
        }
    }
    
    UnitMovement[] FindEnemyUnits()
    {
        List<UnitMovement> enemies = new List<UnitMovement>();
        UnitMovement[] allUnits = FindObjectsOfType<UnitMovement>();
        
        foreach (UnitMovement unit in allUnits)
        {
            Faction faction = unit.GetComponent<Faction>();
            if (faction != null && faction.factionType == FactionType.Player)
            {
                enemies.Add(unit);
            }
        }
        
        return enemies.ToArray();
    }
    
    Building[] FindEnemyBuildings()
    {
        List<Building> enemies = new List<Building>();
        Building[] allBuildings = FindObjectsOfType<Building>();
        
        foreach (Building building in allBuildings)
        {
            Faction faction = building.GetComponent<Faction>();
            if (faction != null && faction.factionType == FactionType.Player)
            {
                enemies.Add(building);
            }
        }
        
        return enemies.ToArray();
    }
    
    #endregion
}
