using UnityEngine;

public class Worker : UnitMovement
{
    protected override void Start()
    {
        base.Start();
    }

    public void OrderMoveTo(Vector3 destination)
    {
        // Le Worker utilise maintenant le système BuildingConstruction unifié
        // Plus besoin de logique de construction ici
        MoveTo(destination);
    }

    // L'ancien système d'extracteur est maintenant géré par BuildingConstruction
    // Ce script devient très simple !
}
