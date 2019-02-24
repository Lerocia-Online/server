namespace Lerocia.Items {
  using UnityEngine;
  using System.Collections.Generic;
  using Potions;
  using Apparel;
  using Weapons;
  
  public static class ItemList {
    public static readonly List<BaseItem> Items = new List<BaseItem> {
      new HealthPotion(
        0,
        "health potion",
        1,
        10,
        20
      ),
      new BaseWeapon(
        1,
        "weapon",
        10,
        20,
        15
      ),
      new BaseApparel(
        2,
        "armor",
        5,
        10,
        10
      )
    };
    
    public static Dictionary<int, GameObject> WorldItems = new Dictionary<int, GameObject>();
  }
}