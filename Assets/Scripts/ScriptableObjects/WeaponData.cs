using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Shift/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    public Sprite icon;

    [Header("Base Stats")]
    public int baseDamage = 10;
    public float baseCooldown = 1.0f;
    public float range = 10f;       // Detection range for auto-aim
    public float projectileSpeed = 10f;
    public float duration = 2f;     // For orbitals or lingering areas

    [Header("Visuals")]
    public GameObject projectilePrefab; // Must have NetworkObject if spawned

    [Header("Behavior")]
    // This string matches the Class Name (e.g. "ProjectileWeapon")
    // We use reflection or a factory to attach the right script
    public string behaviorScript = "ProjectileWeapon";
}
