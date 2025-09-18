using UnityEngine;

public class BuildingSelection : MonoBehaviour
{
    private Renderer buildingRenderer;
    private Material originalMaterial;
    private bool isSelected = false;
    
    void Start()
    {
        buildingRenderer = GetComponent<Renderer>();
        if (buildingRenderer != null)
        {
            originalMaterial = buildingRenderer.material;
        }
    }
    
    public void SetSelected(bool selected, Material selectedMaterial = null)
    {
        if (buildingRenderer == null) return;
        
        isSelected = selected;
        
        if (selected && selectedMaterial != null)
        {
            buildingRenderer.material = selectedMaterial;
        }
        else
        {
            buildingRenderer.material = originalMaterial;
        }
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }
}
