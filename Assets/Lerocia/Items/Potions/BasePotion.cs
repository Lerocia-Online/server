namespace Lerocia.Items.Potions {
  public abstract class BasePotion : BaseItem {
    protected BasePotion(int id, string name, int weight, int value) : base(id, name, weight, value, "Potion") { }
  }
}