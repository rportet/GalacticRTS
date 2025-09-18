using UnityEngine;
using UnityEngine.AI;

public class UnitMovement : MonoBehaviour
{
    [Header("Composants")]
    private Camera mainCamera;
    private NavMeshAgent agent;
    private Renderer unitRenderer;

    [Header("État")]
    private bool isSelected = false;
    private Material originalMaterial;

    [Header("Vitesse")]
    public float normalSpeed = 3.5f;
    public float runSpeed = 6f;
    private bool isRunning = false;

    protected virtual void Start() // PROTECTED VIRTUAL !
    {
        mainCamera = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        unitRenderer = GetComponent<Renderer>();

        if (agent == null)
        {
            Debug.LogError($"NavMeshAgent manquant sur {gameObject.name}!");
        }
        else
        {
            normalSpeed = 3.5f;
            agent.speed = normalSpeed;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;
        }

        if (unitRenderer == null)
        {
            Debug.LogError($"Renderer manquant sur {gameObject.name}!");
        }
        else
        {
            originalMaterial = unitRenderer.material;
        }
    }

    public void SetRunning(bool running)
    {
        isRunning = running;
        if (agent != null)
        {
            agent.speed = running ? runSpeed : normalSpeed;
        }
    }

    public void SetSelected(bool selected, Material selectedMaterial)
    {
        if (unitRenderer == null)
        {
            Debug.LogWarning($"Renderer manquant sur {gameObject.name}");
            return;
        }

        isSelected = selected;
        unitRenderer.material = selected && selectedMaterial != null ? selectedMaterial : originalMaterial;
    }

    public virtual void MoveTo(Vector3 destination)
    {
        if (agent == null)
        {
            Debug.LogError($"NavMeshAgent manquant sur {gameObject.name}!");
            return;
        }

        agent.SetDestination(destination);
        Debug.Log($"{gameObject.name} se déplace vers: {destination}");

        if (isRunning)
        {
            Invoke("StopRunning", 3f);
        }
    }

    void StopRunning()
    {
        SetRunning(false);
    }

    public bool IsSelected() => isSelected;
}
