using UnityEngine;

public enum FactionType
{
    Player,
    AI_Enemy,
    Neutral
}

public class Faction : MonoBehaviour
{
    [Header("Faction")]
    public FactionType factionType = FactionType.Player;
    public Color factionColor = Color.blue;
    public string factionName = "Joueur";
    
    void Start()
    {
        // Appliquer la couleur de faction au matériau
        ApplyFactionColor();
    }
    
    void ApplyFactionColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Material factionMaterial = new Material(renderer.material);
            factionMaterial.color = factionColor;
            renderer.material = factionMaterial;
        }
    }
    
    public bool IsEnemy(Faction other)
    {
        if (other == null) return false;
        
        // Logique d'ennemi simple
        if (factionType == FactionType.Player && other.factionType == FactionType.AI_Enemy) return true;
        if (factionType == FactionType.AI_Enemy && other.factionType == FactionType.Player) return true;
        
        return false;
    }
    
    public bool IsAlly(Faction other)
    {
        if (other == null) return false;
        return factionType == other.factionType;
    }
}
