using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 15f;
    public PlayerMovement.TeamType team;
    public GameObject owner;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        TreeGrowth tree = other.GetComponent<TreeGrowth>();
        if (tree == null) tree = other.GetComponentInParent<TreeGrowth>();
        if (tree == null && (other.CompareTag("Tree") || other.name.Contains("Trunk") || other.name.Contains("Tree")))
        {
            tree = TreeGrowth.Instance;
        }
        if (tree != null)
        {
            float levelMultiplier = TreeGrowth.GetHeightMultiplier(transform.position);
            float finalAmount = damage * levelMultiplier;

            if (team == PlayerMovement.TeamType.Destroyer)
                tree.TakeDamage(finalAmount, owner);
            else
                tree.Heal(finalAmount);

            Destroy(gameObject);
            return;
        }

        Health health = other.GetComponent<Health>();
        if (health == null) health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            CharacterCombat otherCombat = health.GetComponent<CharacterCombat>() ?? health.GetComponentInParent<CharacterCombat>() ?? other.GetComponent<CharacterCombat>() ?? other.GetComponentInParent<CharacterCombat>();
            if (otherCombat != null && otherCombat.team != this.team && !health.isDead)
            {
                health.TakeDamage(damage, owner);
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }
}
