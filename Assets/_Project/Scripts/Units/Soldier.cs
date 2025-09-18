using UnityEngine;
using UnityEngine.AI;

public class Soldier : UnitMovement
{
    [Header("Combat")]
    public int damage = 25;
    public float attackRange = 5f;
    public float attackCooldown = 1.5f;

    [Header("Combat à Distance")]
    public float rangedAttackRange = 8f;
    public float meleeAttackRange = 2f;
    public bool useRangedAttack = true;
    public LineRenderer laserLine;

    private GameObject currentTarget;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private HealthSystem targetHealth;

    protected override void Start() // OVERRIDE avec base.Start()
    {
        base.Start(); // APPELER LE PARENT D'ABORD

        // Créer ligne laser
        laserLine = gameObject.AddComponent<LineRenderer>();
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.endColor = Color.red;
        laserLine.startWidth = 0.1f;
        laserLine.endWidth = 0.05f;
        laserLine.enabled = false;
    }

    void Update()
    {
        HandleAttack();
    }

    public void OrderMoveTo(Vector3 destination)
    {
        if (isAttacking)
        {
            Debug.Log($"{gameObject.name} : Attaque annulée par ordre de déplacement");
            StopAttack();
        }
        MoveTo(destination);
    }

    public void AttackTarget(GameObject target)
    {
        if (target == null) return;

        // VÉRIFIER FACTION AVANT ATTAQUE
        Faction myFaction = GetComponent<Faction>();
        Faction targetFaction = target.GetComponent<Faction>();

        if (myFaction != null && targetFaction != null)
        {
            if (myFaction.IsAlly(targetFaction))
            {
                Debug.Log($"{gameObject.name} refuse d'attaquer allié {target.name}");
                return;
            }

            if (!myFaction.IsEnemy(targetFaction))
            {
                Debug.Log($"{gameObject.name} refuse d'attaquer unité neutre {target.name}");
                return;
            }
        }

        HealthSystem targetHealthSystem = target.GetComponent<HealthSystem>();
        if (targetHealthSystem == null || !targetHealthSystem.IsAlive())
        {
            Debug.Log("Cible invalide ou déjà morte !");
            return;
        }

        if (target == gameObject)
        {
            Debug.Log("Impossible d'attaquer soi-même !");
            return;
        }

        currentTarget = target;
        targetHealth = targetHealthSystem;
        isAttacking = true;

        string targetType = targetHealthSystem.isUnit ? "unité" :
                           targetHealthSystem.isBuilding ? "bâtiment" : "cible";
        Debug.Log($"{gameObject.name} attaque {targetType} : {target.name}");

        MoveTo(target.transform.position);
    }

    void HandleAttack()
    {
        if (!isAttacking || currentTarget == null) return;

        if (currentTarget == null || targetHealth == null || !targetHealth.IsAlive())
        {
            StopAttack();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        float effectiveRange = useRangedAttack ? rangedAttackRange : meleeAttackRange;

        if (distanceToTarget <= effectiveRange)
        {
            // Arrêter le mouvement
            GetComponent<NavMeshAgent>().SetDestination(transform.position);

            // Regarder vers la cible
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Attaquer si cooldown écoulé
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                if (useRangedAttack)
                {
                    PerformRangedAttack();
                }
                else
                {
                    PerformAttack();
                }
            }
        }
        else
        {
            // Continuer à poursuivre
            if (Time.time - lastAttackTime > 0.5f)
            {
                MoveTo(currentTarget.transform.position);
                lastAttackTime = Time.time;
            }
        }
    }

    void PerformRangedAttack()
    {
        if (targetHealth != null && targetHealth.IsAlive())
        {
            StartCoroutine(LaserEffect());
            targetHealth.TakeDamage(damage, gameObject);
            lastAttackTime = Time.time;
            Debug.Log($"{gameObject.name} tire sur {currentTarget.name} ({damage} dégâts)");
        }
        else
        {
            StopAttack();
        }
    }

    void PerformAttack()
    {
        if (targetHealth != null && targetHealth.IsAlive())
        {
            targetHealth.TakeDamage(damage, gameObject);
            lastAttackTime = Time.time;
            Debug.Log($"{gameObject.name} inflige {damage} dégâts à {currentTarget.name}");
            StartCoroutine(AttackAnimation());
        }
        else
        {
            StopAttack();
        }
    }

    System.Collections.IEnumerator LaserEffect()
    {
        if (laserLine != null && currentTarget != null)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, transform.position + Vector3.up);
            laserLine.SetPosition(1, currentTarget.transform.position + Vector3.up);
            yield return new WaitForSeconds(0.1f);
            laserLine.enabled = false;
        }
    }

    System.Collections.IEnumerator AttackAnimation()
    {
        Vector3 originalPosition = transform.position;
        Vector3 attackPosition = originalPosition + transform.forward * 0.3f;

        // Avancer
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            transform.position = Vector3.Lerp(originalPosition, attackPosition, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reculer
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            transform.position = Vector3.Lerp(attackPosition, originalPosition, elapsed / 0.1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    public void StopAttack()
    {
        isAttacking = false;
        currentTarget = null;
        targetHealth = null;
        Debug.Log($"{gameObject.name} arrête l'attaque");
    }
}
