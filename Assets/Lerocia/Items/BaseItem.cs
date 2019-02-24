namespace Lerocia.Items {
  using System.Collections.Generic;
  using Characters;

  public abstract class BaseItem {
    private int id;
    private string name;
    private int weight;
    private int value;
    private string category;
    private string description;
    private List<KeyValuePair<string, string>> stats;

    protected BaseItem(int id, string name, int weight, int value, string category) {
      this.id = id;
      this.name = name;
      this.weight = weight;
      this.value = value;
      this.category = category;
      description = null;
      stats = new List<KeyValuePair<string, string>>();
      stats.Add(new KeyValuePair<string, string>("Weight", weight.ToString()));
      stats.Add(new KeyValuePair<string, string>("Value", value.ToString()));
    }

    public int GetId() {
      return id;
    }

    public string GetName() {
      return name;
    }

    public int GetWeight() {
      return weight;
    }

    public int GetValue() {
      return value;
    }

    public string GetCategory() {
      return category;
    }

    public string GetDescription() {
      return description;
    }

    protected void SetDescription(string description) {
      this.description = description;
    }

    protected void AddStat(string title, string value) {
      stats.Add(new KeyValuePair<string, string>(title, value));
    }

    public List<KeyValuePair<string, string>> GetStats() {
      return stats;
    }

    public abstract void Use(Character character);

    public void Drop(Character character) {
      character.Inventory.Remove(id);
    }
  }
}