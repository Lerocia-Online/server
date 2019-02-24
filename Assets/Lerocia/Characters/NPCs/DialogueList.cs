namespace Lerocia.Characters.NPCs {
  using System.Collections.Generic;

  public static class DialogueList {
    public static readonly Dictionary<int, Dictionary<string, Dialogue>> Dialogues =
      new Dictionary<int, Dictionary<string, Dialogue>> {
        {
          0,
          new Dictionary<string, Dialogue> {
            {
              "INTERACT",
              new Dialogue("",
                new[] {
                  "LootBody"
                })
            }
          }
        }, {
          1,
          new Dictionary<string, Dialogue> {
            {
              "INTERACT",
              new Dialogue("Hi! I'm a friendly NPC, want to talk?",
                new[] {
                  "Yea, sure.",
                  "Nope."
                })
            }, {
              "Yea, sure.",
              new Dialogue("Cool! I don't have a whole lot to talk about but if you kill me you can take all my items!",
                new[] {
                  "That's kind of messed up...",
                  "Thanks for the tip."
                })
            }, {
              "Nope.",
              new Dialogue("Ok, have a good one.", null)
            }, {
              "That's kind of messed up...",
              new Dialogue("It is a little bit, but that's just how this game works. Happy killing!", null)
            }, {
              "Thanks for the tip.",
              new Dialogue("No problem!", null)
            }
          }
        }, {
          2,
          new Dictionary<string, Dialogue> {
            {
              "INTERACT",
              new Dialogue("Some NPC's are merchants, like me. Want to buy something?",
                new[] {
                  "Show me what you've got!",
                  "Not today."
                })
            }, {
              "Show me what you've got!",
              new Dialogue("Sorry, merchant functionality isn't implemented yet. Keep an eye out for future updates!",
                new[] {
                  "StartMerchant"
                })
            }, {
              "Not today.",
              new Dialogue("Come back anytime!", null)
            }
          }
        }, {
          3,
          new Dictionary<string, Dialogue> {
            {
              "INTERACT",
              new Dialogue("Not all NPC's are up for a conversation, don't take it personally.", null)
            }
          }
        }
      };
  }
}