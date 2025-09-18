// ResourceNode.cs
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Credits, Energie }
    public ResourceType type;
    public int supplyRate = 20; // unités par seconde
}
