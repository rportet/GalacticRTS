// ExtractorBehavior.cs
using UnityEngine;

public class ExtractorBehavior : MonoBehaviour
{
    private ResourceNode node;
    private ResourceManager rm;

    void Start()
    {
        rm = ResourceManager.Instance;
        // Trouver le node le plus proche
        ResourceNode[] allNodes = FindObjectsOfType<ResourceNode>();
        float minDist = float.MaxValue;
        foreach (var n in allNodes)
        {
            float d = Vector3.Distance(transform.position, n.transform.position);
            if (d < minDist)
            {
                minDist = d;
                node = n;
            }
        }
    }

    void Update()
    {
        if (node == null || rm == null) return;
        // Ajout passif chaque seconde
        // On peut déléguer la cadence à ResourceManager si besoin
        if (Time.frameCount % 60 == 0) // approximativement chaque seconde
        {
            if (node.type == ResourceNode.ResourceType.Credits)
                rm.AddCredits(node.supplyRate);
            else
                rm.AddEnergie(node.supplyRate);
        }
    }
}
