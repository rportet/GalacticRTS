using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Configuration Initiale")]
    public GameObject playerHQPrefab;
    public GameObject aiHQPrefab;
    public GameObject workerPrefab;
    public GameObject soldierPrefab;
    
    [Header("Positions de Départ")]
    public Transform playerStartPosition;
    public Transform aiStartPosition;
    
    void Start()
    {
        SetupGame();
    }
    
    void SetupGame()
    {
        // Créer base joueur
        CreatePlayerBase();
        
        // Créer base IA
        CreateAIBase();
        
        Debug.Log("Partie initialisée - Joueur vs IA !");
    }
    
    void CreatePlayerBase()
    {
        Vector3 playerPos = playerStartPosition != null ? playerStartPosition.position : new Vector3(-10, 0, 0);
        
        // QG Joueur
        GameObject playerHQ = Instantiate(playerHQPrefab, playerPos, Quaternion.identity);
        Faction hqFaction = playerHQ.AddComponent<Faction>();
        hqFaction.factionType = FactionType.Player;
        hqFaction.factionColor = Color.blue;
        hqFaction.factionName = "Joueur";
        
        // Worker Joueur
        GameObject playerWorker = Instantiate(workerPrefab, playerPos + Vector3.right * 3, Quaternion.identity);
        Faction workerFaction = playerWorker.AddComponent<Faction>();
        workerFaction.factionType = FactionType.Player;
        workerFaction.factionColor = Color.blue;
        
        // Soldier Joueur
        GameObject playerSoldier = Instantiate(soldierPrefab, playerPos + Vector3.forward * 3, Quaternion.identity);
        Faction soldierFaction = playerSoldier.AddComponent<Faction>();
        soldierFaction.factionType = FactionType.Player;
        soldierFaction.factionColor = Color.blue;
        
        Debug.Log("Base joueur créée");
    }
    
    void CreateAIBase()
    {
        Vector3 aiPos = aiStartPosition != null ? aiStartPosition.position : new Vector3(10, 0, 0);
        
        // QG IA
        GameObject aiHQ = Instantiate(aiHQPrefab, aiPos, Quaternion.identity);
        Faction hqFaction = aiHQ.AddComponent<Faction>();
        hqFaction.factionType = FactionType.AI_Enemy;
        hqFaction.factionColor = Color.red;
        hqFaction.factionName = "IA";
        
        // Worker IA
        GameObject aiWorker = Instantiate(workerPrefab, aiPos + Vector3.left * 3, Quaternion.identity);
        Faction workerFaction = aiWorker.AddComponent<Faction>();
        workerFaction.factionType = FactionType.AI_Enemy;
        workerFaction.factionColor = Color.red;
        
        // Soldier IA
        GameObject aiSoldier = Instantiate(soldierPrefab, aiPos + Vector3.back * 3, Quaternion.identity);
        Faction soldierFaction = aiSoldier.AddComponent<Faction>();
        soldierFaction.factionType = FactionType.AI_Enemy;
        soldierFaction.factionColor = Color.red;
        
        // Ajouter IA Controller
        GameObject aiController = new GameObject("AI_Controller");
        AIController ai = aiController.AddComponent<AIController>();
        ai.aiHeadquarters = aiHQ.GetComponent<Building>();
        
        Debug.Log("Base IA créée avec controller");
    }
}
