using UnityEngine;

public class ExtractorBuilding : MonoBehaviour
{
    [Header("Extracteur")]
    public ResourceNode linkedNode;
    
    void Start()
    {
        // Trouver le ResourceNode le plus proche
        FindNearestResourceNode();
        
        // Ajouter système de sélection
        BuildingSelection buildingSelection = gameObject.AddComponent<BuildingSelection>();
    }
    
    void FindNearestResourceNode()
    {
        ResourceNode[] nodes = FindObjectsOfType<ResourceNode>();
        float minDistance = float.MaxValue;
        
        foreach (ResourceNode node in nodes)
        {
            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                linkedNode = node;
            }
        }
        
        if (linkedNode != null && minDistance < 3f)
        {
            Debug.Log($"Extracteur lié au ResourceNode {linkedNode.type}");
        }
        else
        {
            Debug.LogWarning("Extracteur construit sans ResourceNode proche !");
        }
    }
}
