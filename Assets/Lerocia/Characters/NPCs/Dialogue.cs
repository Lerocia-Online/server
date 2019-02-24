namespace Lerocia.Characters.NPCs {
  public class Dialogue {
    public string response;
    public string[] options;
    public Dialogue(string response, string[] options) {
      this.response = response;
      this.options = options;
    }
  }
}