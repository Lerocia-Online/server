namespace Lerocia.Characters.Players {
  using UnityEngine;

  public class Player : Character {
    public Player() { }

    public Player(string name, GameObject avatar, string type, int maxHealth, int currentHealth, int maxStamina,
      int currentStamina, int gold, int baseDamage, int baseArmor, int weapon, int apparel) : base(
      name, avatar, type, maxHealth, currentHealth, maxStamina, currentStamina, gold, baseDamage, baseArmor, weapon,
      apparel) { }

    protected override void Kill() {
      //TODO Handle Player death
      // Reset players health
      CurrentHealth = MaxHealth;
      // Move them back to "spawn" point
      Avatar.transform.position = new Vector3(0, 1, 0);
    }
  }
}