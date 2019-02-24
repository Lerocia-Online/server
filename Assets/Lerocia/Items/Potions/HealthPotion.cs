namespace Lerocia.Items.Potions {
  using Characters;

  public class HealthPotion : BasePotion {
    private int health;

    public HealthPotion(int id, string name, int weight, int value, int health) : base(id, name, weight, value) {
      this.health = health;
      SetDescription("Heals by " + health + " points.");
    }

    public override void Use(Character character) {
      character.CurrentHealth += health;
      if (character.CurrentHealth >= character.MaxHealth) {
        character.CurrentHealth = character.MaxHealth;
      }
      character.Inventory.Remove(GetId());
    }
  }
}