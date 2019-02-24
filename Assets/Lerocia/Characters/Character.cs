namespace Lerocia.Characters {
  using System.Collections.Generic;
  using UnityEngine;
  using Items;
  using Items.Weapons;
  using Items.Apparel;

  public abstract class Character {
    // Identifiers
    public int UserId;
    public string Name;
    public GameObject Avatar;
    public string Type;

    // Movement interpolation
    public bool IsLerpingPosition;
    public bool IsLerpingRotation;
    public Vector3 RealPosition;
    public Quaternion RealRotation;
    public Vector3 LastRealPosition;
    public Quaternion LastRealRotation;
    public float TimeStartedLerping;
    public float TimeToLerp;
    public float TimeBetweenMovementStart;
    public float TimeBetweenMovementEnd;
    public float MoveTime;

    // Stats
    public int MaxHealth;
    public int CurrentHealth;
    public int MaxStamina;
    public int CurrentStamina;
    public int Gold;
    public int Weight;
    public int BaseDamage;
    public int Damage;
    public int BaseArmor;
    public int Armor;
    public bool IsDead;

    // Equipped armor & weapons
    public int Weapon;
    public int Apparel;

    public List<int> Inventory;

    public Character() {
      Avatar = new GameObject();
      Inventory = new List<int>();
    }

    public Character(string name, GameObject avatar, string type, int maxHealth, int currentHealth, int maxStamina,
      int currentStamina, int gold, int baseDamage, int baseArmor, int weapon, int apparel) {
      Name = name;
      Avatar = avatar;
      Type = type;
      IsLerpingPosition = false;
      IsLerpingRotation = false;
      RealPosition = avatar.transform.position;
      RealRotation = avatar.transform.rotation;
      TimeBetweenMovementStart = Time.time;
      MaxHealth = maxHealth;
      CurrentHealth = currentHealth;
      MaxStamina = maxStamina;
      CurrentStamina = currentStamina;
      Weight = 0;
      Gold = gold;
      BaseDamage = baseDamage;
      Damage = BaseDamage;
      BaseArmor = baseArmor;
      Armor = BaseArmor;
      IsDead = false;
      Weapon = weapon;
      Apparel = apparel;
      Inventory = new List<int>();
    }

    public void UpdateStats() {
      if (Weapon >= 0) {
        BaseWeapon weapon = ItemList.Items[Weapon] as BaseWeapon;
        Damage = weapon.GetDamage();
      } else {
        Damage = BaseDamage;
      }

      if (Apparel >= 0) {
        BaseApparel apparel = ItemList.Items[Apparel] as BaseApparel;
        Armor = apparel.GetArmor();
      } else {
        Armor = BaseArmor;
      }
    }

    public void TakeDamage(int damage) {
      if (!IsDead) {
        damage = damage - Armor;
        if (damage <= 0) {
          damage = 0;
        }

        CurrentHealth -= damage;
        if (CurrentHealth <= 0) {
          Kill();
        }
      }
    }

    protected abstract void Kill();
  }
}