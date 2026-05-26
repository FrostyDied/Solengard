using UnityEngine;

public enum WeaponType { Sword, Bow, Staff, Axe, Spear }

// Arma equipada pelo player. Attach junto ao PlayerAttack no GameObject do player.
// Nível vai de 1 a 5; Upgrade() melhora os atributos progressivamente.
public class PlayerWeapon : MonoBehaviour
{
    public static event System.Action<PlayerWeapon> OnWeaponUpgraded;

    [Header("Identificação")]
    public string weaponName = "Espada";
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Atributos Base")]
    public float damage      = 20f;
    public float attackRange = 3f;
    public float attackSpeed = 1f;

    [Header("Nível (1-5)")]
    [Range(1, 5)] public int level = 1;

    [Header("Evolução")]
    // Item passivo necessário para evoluir esta arma via WeaponEvolutionSystem
    public PassiveItemType evolutionRequirement = PassiveItemType.DamageGem;

    public bool IsMaxLevel => level >= 5;

    // Incrementa o nível e aplica melhorias proporcionais nos atributos
    public void Upgrade()
    {
        if (IsMaxLevel)
        {
            Debug.Log($"[PlayerWeapon] {weaponName} já está no nível máximo.");
            return;
        }

        level++;
        damage      *= 1.2f;
        attackRange *= 1.05f;
        attackSpeed *= 1.1f;

        Debug.Log($"[PlayerWeapon] {weaponName} → Nível {level} | Dano: {damage:F1}");
        OnWeaponUpgraded?.Invoke(this);
    }

    public PassiveItemType GetEvolutionRequirement() => evolutionRequirement;
}
