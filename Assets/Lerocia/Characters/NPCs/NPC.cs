namespace Lerocia.Characters.NPCs {
  using UnityEngine;
  using System.Collections.Generic;

  public class NPC : Character {
    protected Dictionary<string, Dialogue> _dialogues;
    public int DialogueId;

    public NPC() { }

    public NPC(string name, GameObject avatar, string type, int maxHealth, int currentHealth, int maxStamina,
      int currentStamina, int gold, int baseDamage, int baseArmor, int weapon, int apparel,
      int dialogueId) : base(name, avatar, type, maxHealth, currentHealth, maxStamina, currentStamina, gold, baseDamage,
      baseArmor, weapon, apparel) {
      DialogueId = dialogueId;
      _dialogues = DialogueList.Dialogues[dialogueId];
    }

    public virtual string[] Interact(string prompt) {
      //TODO Handle NPC interaction
      return null;
    }

    protected override void Kill() {
      //TODO Handle NPC death
      IsDead = true;
      _dialogues = DialogueList.Dialogues[0];
    }

    public virtual void StartMerchant() {
      //TODO Handle NPC start merchant
    }

    public virtual void LootBody() {
      //TODO Handle NPC loot body
    }
  }
}