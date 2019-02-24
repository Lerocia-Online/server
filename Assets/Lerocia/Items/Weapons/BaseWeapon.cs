namespace Lerocia.Items.Weapons {
  using Characters;

  public class BaseWeapon : BaseItem {
    private int damage;

    public BaseWeapon(int id, string name, int weight, int value, int damage) : base(id, name, weight, value, "Weapon") {
      this.damage = damage;
      AddStat("Damage", damage.ToString());
    }

    public int GetDamage() {
      return damage;
    }

    private void Equip(Character character) {
      if (character.Weapon != GetId()) {
        character.Weapon = GetId();
      } else {
        character.Weapon = -1;
      }
      character.UpdateStats();
    }

    public override void Use(Character character) {
      Equip(character);
    }
  }
}