namespace Lerocia.Characters.Players {
  using System;

  [Serializable]
  public class User {
    public bool success;
    public string error;
    public string username;
    public int user_id;
    public int connection_id;
  }
}