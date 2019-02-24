namespace Lerocia.Characters {
  using System.Collections.Generic;
  using Players;
  using NPCs;

  public static class ConnectedCharacters {
    public static User MyUser;
    public static Player MyPlayer;
    public static List<Character> Characters = new List<Character>();
    public static Dictionary<int, Player> Players = new Dictionary<int, Player>();
    public static Dictionary<int, NPC> NPCs = new Dictionary<int, NPC>();
  }
}
