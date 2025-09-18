using UnityEngine;

public class Barracks : Building
{
    void Start()
    {
        // Configuration spécifique à la caserne
        buildingName = "Caserne";
        maxHealth = 300;
        currentHealth = maxHealth;
        
        // Récupérer les prefabs depuis les ressources ou FindObjectOfType
        GameObject soldierPrefab = null;
        GameObject heavySoldierPrefab = null;
        
        // Méthode 1: Chercher dans les GameObjects existants
        Building[] allBuildings = FindObjectsOfType<Building>();
        foreach (Building building in allBuildings)
        {
            if (building.availableUnits != null)
            {
                foreach (var unitType in building.availableUnits)
                {
                    if (unitType.name == "Soldier" && unitType.prefab != null)
                        soldierPrefab = unitType.prefab;
                    if (unitType.name == "HeavySoldier" && unitType.prefab != null)
                        heavySoldierPrefab = unitType.prefab;
                }
            }
        }
        
        // Types d'unités que la caserne peut produire
        availableUnits = new UnitType[]
        {
            new UnitType { 
                name = "Soldier", 
                prefab = soldierPrefab, 
                cost = 75, 
                productionTime = 2f, 
                shortcut = KeyCode.S 
            },
            new UnitType { 
                name = "HeavySoldier", 
                prefab = heavySoldierPrefab, 
                cost = 120, 
                productionTime = 3f, 
                shortcut = KeyCode.H 
            }
        };
        
        // Appeler Start du parent
        base.Start();
        
        Debug.Log($"{buildingName} initialisé avec {availableUnits.Length} types d'unités");
        
        // Debug des prefabs
        foreach (var unit in availableUnits)
        {
            Debug.Log($"Caserne - {unit.name}: {(unit.prefab != null ? "OK" : "MANQUANT")}");
        }
    }
}
