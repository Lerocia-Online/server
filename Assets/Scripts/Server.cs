using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Lerocia.Characters;
using Lerocia.Characters.NPCs;
using Lerocia.Characters.Players;
using Lerocia.Items;
using Networking.Constants;
using Random = System.Random;

class ServerPlayer : Player {
  private Server _myServer;

  public ServerPlayer() {
    Inventory.BeforeRemove += RequestItemDeletion;
    _myServer = GameObject.Find("Server").GetComponent<Server>();
  }

  private void RequestItemDeletion(int deletedItem) {
    int[] args = {UserId, deletedItem};
    _myServer.StartCoroutine("DeleteItemForUser", args);
  }

  public override void InitializeOnInventoryChange() {
    Inventory.ListChanged += OnInventoryChange;
  }

  protected override void OnInventoryChange(object sender, ListChangedEventArgs e) {
    if (e.ListChangedType == ListChangedType.ItemAdded) {
      int[] args = {UserId, Inventory[e.NewIndex]};
      _myServer.StartCoroutine("AddItemForUser", args);
    }
  }
}

[Serializable]
class DatabaseUser {
  public bool success;
  public string error;
  public int user_id;
  public string username;
  public float position_x;
  public float position_y;
  public float position_z;
  public float rotation_x;
  public float rotation_y;
  public float rotation_z;
  public string type;
  public int max_health;
  public int current_health;
  public int max_stamina;
  public int current_stamina;
  public int gold;
  public int equipped_weapon;
  public int equipped_apparel;
}

[Serializable]
class DatabaseWorldItem {
  public int world_id;
  public int item_id;
  public float position_x;
  public float position_y;
  public float position_z;
}

[Serializable]
public class DatabaseItem {
  public int item_id;
}

[Serializable]
class DatabaseNPC {
  public int npc_id;
  public string npc_name;
  public float position_x;
  public float position_y;
  public float position_z;
  public float rotation_x;
  public float rotation_y;
  public float rotation_z;
  public string type;
  public int dialogue_id;
}

public class Server : MonoBehaviour {
  private const int MAX_CONNECTION = 100;

  private int port = NetworkConstants.Port;

  private int hostId;

  private int reliableChannel;
  private int unreliableChannel;

  private bool isStarted = false;
  private byte error;

  private float lastMovementUpdate;
  private float movementUpdateRate = 0.5f;

  private WWWForm form;
  private string getWorldItemsEndpoint = "get_world_items.php";
  private string getNPCsEndpoint = "get_npcs.php";
  private string getItemsForUserEndpoint = "get_items_for_user.php";
  private string getStatsForUserEndpoint = "get_stats_for_user.php";
  private string setstatsForUserEndpoint = "set_stats_for_user.php";
  private string getItemsForNPCEndpoint = "get_items_for_npc.php";
  private string addItemForUserEndpoint = "add_item_for_user.php";
  private string deleteItemForUserEndpoint = "delete_item_for_user.php";
  private string addWorldItemEndpoint = "add_world_item.php";
  private string deleteWorldItemEndpoint = "delete_world_item.php";
  private string logoutEndpoint = "logout.php";
  private string logoutAllUsersEndpoint = "logout_all_users.php";

  private void Awake() {
    StartCoroutine("LogoutAllUsers");
  }

  private void Start() {
    NetworkTransport.Init();
    ConnectionConfig cc = new ConnectionConfig();

    reliableChannel = cc.AddChannel(QosType.Reliable);
    unreliableChannel = cc.AddChannel(QosType.Unreliable);

    HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

    hostId = NetworkTransport.AddHost(topo, NetworkConstants.Port, null);

    isStarted = true;
    StartCoroutine("GetWorldItems");
    StartCoroutine("GetNPCs");
  }

  private IEnumerator GetNPCs() {
    form = new WWWForm();

    WWW w = new WWW(NetworkConstants.Api + getNPCsEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseNPC[] dbnpc = JsonHelper.FromJson<DatabaseNPC>(jsonString);
      foreach (DatabaseNPC npc in dbnpc) {
        AddNPC(npc.npc_id, npc.npc_name, npc.position_x, npc.position_y, npc.position_z, npc.rotation_x, npc.rotation_y,
          npc.rotation_z, npc.type, npc.dialogue_id);
      }
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetWorldItems() {
    form = new WWWForm();

    WWW w = new WWW(NetworkConstants.Api + getWorldItemsEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseWorldItem[] dbi = JsonHelper.FromJson<DatabaseWorldItem>(jsonString);
      foreach (DatabaseWorldItem it in dbi) {
        AddWorldItem(it.world_id, it.item_id, it.position_x, it.position_y, it.position_z, true);
      }
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetItemsForUser(int[] ids) {
    form = new WWWForm();
    int userId = ids[0];
    int cnnId = ids[1];

    form.AddField("user_id", userId);

    WWW w = new WWW(NetworkConstants.Api + getItemsForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseItem[] dbi = JsonHelper.FromJson<DatabaseItem>(jsonString);
      foreach (DatabaseItem it in dbi) {
        ConnectedCharacters.Players[cnnId].Inventory.Add(it.item_id);
      }

      ConnectedCharacters.Players[cnnId].InitializeOnInventoryChange();

      string inventoryMessage = "INVENTORY|";
      foreach (int itemId in ConnectedCharacters.Players[cnnId].Inventory) {
        inventoryMessage += itemId + "|";
      }

      inventoryMessage = inventoryMessage.Trim('|');

      Send(inventoryMessage, reliableChannel, cnnId);
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetStatsForUser(int[] ids) {
    form = new WWWForm();
    int userId = ids[0];
    int cnnId = ids[1];

    form.AddField("user_id", userId);

    WWW w = new WWW(NetworkConstants.Api + getStatsForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      DatabaseUser dbu = JsonUtility.FromJson<DatabaseUser>(w.text);
      ConnectedCharacters.Players[cnnId].Avatar.transform.position =
        new Vector3(dbu.position_x, dbu.position_y, dbu.position_z);
      ConnectedCharacters.Players[cnnId].Avatar.transform.rotation =
        Quaternion.Euler(new Vector3(dbu.rotation_x, dbu.rotation_y, dbu.rotation_z));
      ConnectedCharacters.Players[cnnId].Type = dbu.type;
      ConnectedCharacters.Players[cnnId].MaxHealth = dbu.max_health;
      ConnectedCharacters.Players[cnnId].CurrentHealth = dbu.current_health;
      ConnectedCharacters.Players[cnnId].MaxStamina = dbu.max_stamina;
      ConnectedCharacters.Players[cnnId].CurrentStamina = dbu.current_stamina;
      ConnectedCharacters.Players[cnnId].Gold = dbu.gold;
      ConnectedCharacters.Players[cnnId].Weapon = dbu.equipped_weapon;
      ConnectedCharacters.Players[cnnId].Apparel = dbu.equipped_apparel;

      // Tell everybody that a new player has connected
      Send(
        "CNN|" + dbu.username + '|' + cnnId + '|' + dbu.position_x + '|' + dbu.position_y + '|' + dbu.position_z + '|' +
        dbu.rotation_x + '|' + dbu.rotation_y + '|' + dbu.rotation_z + '|' + dbu.type + '|' + dbu.equipped_weapon +
        '|' + dbu.equipped_apparel + '|' + dbu.max_health + '|' + dbu.current_health + '|' + dbu.max_stamina + '|' +
        dbu.current_stamina + '|' + dbu.gold, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator SetStatsForUser(Player player) {
    form = new WWWForm();
    form.AddField("user_id", player.UserId);
    form.AddField("position_x", player.Avatar.transform.position.x.ToString());
    form.AddField("position_y", player.Avatar.transform.position.y.ToString());
    form.AddField("position_z", player.Avatar.transform.position.z.ToString());
    form.AddField("rotation_x", player.Avatar.transform.rotation.eulerAngles.x.ToString());
    form.AddField("rotation_y", player.Avatar.transform.rotation.eulerAngles.y.ToString());
    form.AddField("rotation_z", player.Avatar.transform.rotation.eulerAngles.z.ToString());
    form.AddField("type", player.Type);
    form.AddField("max_health", player.MaxHealth);
    form.AddField("current_health", player.CurrentHealth);
    form.AddField("max_stamina", player.MaxStamina);
    form.AddField("current_stamina", player.CurrentStamina);
    form.AddField("gold", player.Gold);
    form.AddField("equipped_weapon", player.Weapon);
    form.AddField("equipped_apparel", player.Apparel);

    WWW w = new WWW(NetworkConstants.Api + setstatsForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, logout successful
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetItemsForNPC(int npcId) {
    form = new WWWForm();

    form.AddField("npc_id", npcId);

    WWW w = new WWW(NetworkConstants.Api + getItemsForNPCEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseItem[] dbi = JsonHelper.FromJson<DatabaseItem>(jsonString);
      foreach (DatabaseItem it in dbi) {
        ConnectedCharacters.NPCs[npcId].Inventory.Add(it.item_id);
      }
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator AddItemForUser(int[] args) {
    form = new WWWForm();
    int userId = args[0];
    int itemId = args[1];

    form.AddField("user_id", userId);
    form.AddField("item_id", itemId);

    WWW w = new WWW(NetworkConstants.Api + addItemForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, added successfully
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator DeleteItemForUser(int[] args) {
    form = new WWWForm();
    int userId = args[0];
    int itemId = args[1];

    form.AddField("user_id", userId);
    form.AddField("item_id", itemId);

    WWW w = new WWW(NetworkConstants.Api + deleteItemForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, deleted successfully
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator AddWorldItemCoroutine(float[] args) {
    form = new WWWForm();
    int worldId = (int) args[0];
    int itemId = (int) args[1];
    float x = args[2];
    float y = args[3];
    float z = args[4];

    form.AddField("world_id", worldId);
    form.AddField("item_id", itemId);
    form.AddField("position_x", x.ToString());
    form.AddField("position_y", y.ToString());
    form.AddField("position_z", z.ToString());

    WWW w = new WWW(NetworkConstants.Api + addWorldItemEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, added successfully
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator DeleteWorldItemCoroutine(int worldId) {
    form = new WWWForm();
    form.AddField("world_id", worldId);

    WWW w = new WWW(NetworkConstants.Api + deleteWorldItemEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, deleted successfully
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator Logout(int userId) {
    form = new WWWForm();
    form.AddField("user_id", userId);

    WWW w = new WWW(NetworkConstants.Api + logoutEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, logout successful
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator LogoutAllUsers() {
    form = new WWWForm();

    WWW w = new WWW(NetworkConstants.Api + logoutAllUsersEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, logout successful
    } else {
      Debug.Log(w.error);
    }
  }

  private void Update() {
    if (!isStarted) {
      return;
    }

    int recHostId;
    int connectionId;
    int channelId;
    byte[] recBuffer = new byte[1024];
    int bufferSize = 1024;
    int dataSize;
    byte error;
    NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer,
      bufferSize, out dataSize, out error);
    switch (recData) {
      case NetworkEventType.ConnectEvent:
        Debug.Log("Player " + connectionId + " has connected");
        OnConnection(connectionId);
        break;
      case NetworkEventType.DataEvent:
        string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
        Debug.Log("Receiving from " + connectionId + " has sent : " + msg);
        string[] splitData = msg.Split('|');
        switch (splitData[0]) {
          case "NAMEIS":
            OnNameIs(connectionId, splitData[1], int.Parse(splitData[2]));
            break;
          case "MYPOSITION":
            OnMyPosition(connectionId, float.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]),
              float.Parse(splitData[4]), float.Parse(splitData[5]), float.Parse(splitData[6]),
              float.Parse(splitData[7]), float.Parse(splitData[8]));
            break;
          case "ATK":
            OnAttack(connectionId);
            break;
          case "HIT":
            OnHit(connectionId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          case "HITNPC":
            OnHitNPC(connectionId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          case "USE":
            OnUse(connectionId, int.Parse(splitData[1]));
            break;
          case "DROP":
            OnDrop(connectionId, int.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]),
              float.Parse(splitData[4]));
            break;
          case "PICKUP":
            OnPickup(connectionId, int.Parse(splitData[1]));
            break;
          case "NPCITEMS":
            OnNPCItems(connectionId, int.Parse(splitData[1]));
            break;
          default:
            Debug.Log("Invalid message : " + msg);
            break;
        }

        break;
      case NetworkEventType.DisconnectEvent:
        Debug.Log("Player " + connectionId + " has disconnected");
        OnDisconnection(connectionId);
        break;
    }

    // Ask player for their position
    if (Time.time - lastMovementUpdate > movementUpdateRate) {
      lastMovementUpdate = Time.time;
      string m = "ASKPOSITION|";
      foreach (int cnnId in ConnectedCharacters.Players.Keys) {
        Player player = ConnectedCharacters.Players[cnnId];
        m += cnnId.ToString() + '%' + player.Avatar.transform.position.x.ToString() + '%' +
             player.Avatar.transform.position.y.ToString() + '%' +
             player.Avatar.transform.position.z.ToString() + '%' + player.Avatar.transform.rotation.w.ToString() + '%' +
             player.Avatar.transform.rotation.x.ToString() + '%' +
             player.Avatar.transform.rotation.y.ToString() + '%' + player.Avatar.transform.rotation.z.ToString() + '%' +
             player.MoveTime.ToString() + '|';
      }

      m = m.Trim('|');
      Send(m, unreliableChannel, ConnectedCharacters.Players.Keys.ToList());
    }
  }

  private void OnConnection(int cnnId) {
    // Add him to a list
    Player player = new ServerPlayer();
    player.Name = "TEMP";
    ConnectedCharacters.Players.Add(cnnId, player);

    // When the player joins the server, tell him his ID
    // Request his name and send the name of all the other players
    string msg = "ASKNAME|" + cnnId + "|";
    foreach (int connectionId in ConnectedCharacters.Players.Keys) {
      Player p = ConnectedCharacters.Players[connectionId];
      msg += p.Name + "%" + connectionId + '%' + p.Avatar.transform.position.x + '%' + p.Avatar.transform.position.y +
             '%' + p.Avatar.transform.position.z + '%' + p.Avatar.transform.rotation.eulerAngles.x + '%' +
             p.Avatar.transform.rotation.eulerAngles.y + '%' + p.Avatar.transform.rotation.eulerAngles.z + '%' +
             p.Type + '%' + p.Weapon + '%' + p.Apparel + '%' + p.MaxHealth + '%' + p.CurrentHealth + '%' +
             p.MaxStamina + '%' + p.CurrentStamina + '%' + p.Gold + "|";
    }

    msg = msg.Trim('|');

    // ASKNAME|3|DAVE%1|MICHAEL%2|TEMP%3
    Send(msg, reliableChannel, cnnId);

    // Send all items placed in the world
    string itemsMessage = "ITEMS|";
    foreach (GameObject item in ItemList.WorldItems.Values) {
      itemsMessage += item.GetComponent<ItemReference>().WorldId + "%" + item.GetComponent<ItemReference>().ItemId +
                      "%" + item.transform.position.x + "%" + item.transform.position.y + "%" +
                      item.transform.position.z + "|";
    }

    itemsMessage = itemsMessage.Trim('|');

    // ITEMS|0%3%10.12%42.11%4.82|1%2%10.12%42.11%4.82|2%3%10.12%42.11%4.82
    Send(itemsMessage, reliableChannel, cnnId);

    // Send all NPCs in the world
    string npcsMessage = "NPCS|";
    foreach (int npcId in ConnectedCharacters.NPCs.Keys) {
      NPC npc = ConnectedCharacters.NPCs[npcId];
      npcsMessage += npcId + "%" + npc.Name + "%" + npc.Avatar.transform.position.x + "%" +
                     npc.Avatar.transform.position.y + "%" +
                     npc.Avatar.transform.position.z + "%" + npc.Avatar.transform.rotation.eulerAngles.x + "%" +
                     npc.Avatar.transform.rotation.eulerAngles.y + "%" +
                     npc.Avatar.transform.rotation.eulerAngles.z + "%" + npc.Type + "%" + npc.DialogueId + "|";
    }

    npcsMessage = npcsMessage.Trim('|');

    Debug.Log("Sending " + npcsMessage);
    // NPCS|0%Harold|1%Johnny|2%Michelle
    Send(npcsMessage, reliableChannel, cnnId);
  }

  private void OnDisconnection(int cnnId) {
    // Update players transform
    StartCoroutine("SetStatsForUser", ConnectedCharacters.Players[cnnId]);
    // Logout player
    StartCoroutine("Logout", ConnectedCharacters.Players[cnnId].UserId);

    // Remove this player from our client list
    ConnectedCharacters.Players.Remove(cnnId);

    // Tell everyone that somebody else has disconnected
    Send("DC|" + cnnId, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnNameIs(int cnnId, string playerName, int userId) {
    // Link the name to the connection Id
    ConnectedCharacters.Players[cnnId].Name = playerName;
    ConnectedCharacters.Players[cnnId].UserId = userId;
    int[] ids = {userId, cnnId};
    StartCoroutine("GetStatsForUser", ids);
    StartCoroutine("GetItemsForUser", ids);
  }

  private void OnMyPosition(int cnnId, float x, float y, float z, float rw, float rx, float ry, float rz, float time) {
    ConnectedCharacters.Players[cnnId].Avatar.transform.position = new Vector3(x, y, z);
    ConnectedCharacters.Players[cnnId].Avatar.transform.rotation = new Quaternion(rx, ry, rz, rw);
    ConnectedCharacters.Players[cnnId].MoveTime = time;
  }

  private void OnAttack(int cnnId) {
    string msg = "ATK|" + cnnId;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnHit(int cnnId, int hitId, int damage) {
    //TODO Remove health from client on server
    string msg = "HIT|" + cnnId + "|" + hitId + "|" + damage;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnHitNPC(int cnnId, int hitId, int damage) {
    //TODO Remove health from NPC on server
    string msg = "HITNPC|" + cnnId + "|" + hitId + "|" + damage;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnUse(int cnnId, int itemId) {
    //TODO Fix this method to 'Use' items properly...
    ItemList.Items[itemId].Use(ConnectedCharacters.Players[cnnId]);
    string msg = "USE|" + cnnId + "|" + itemId;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnDrop(int cnnId, int itemId, float x, float y, float z) {
    ConnectedCharacters.Players[cnnId].Inventory.Remove(itemId);
    Random random = new Random();
    int worldId = random.Next();

    AddWorldItem(worldId, itemId, x, y, z);
    string msg = "DROP|" + cnnId + "|" + worldId + "|" + itemId + "|" + x + "|" + y + "|" + z;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnPickup(int cnnId, int worldId) {
    ConnectedCharacters.Players[cnnId].Inventory.Add(ItemList.WorldItems[worldId].GetComponent<ItemReference>().ItemId);
    DeleteWorldItem(worldId);
    string msg = "PICKUP|" + cnnId + "|" + worldId;
    Send(msg, reliableChannel, ConnectedCharacters.Players.Keys.ToList());
  }

  private void OnNPCItems(int cnnId, int npcId) {
    string message = "NPCITEMS|" + npcId + "|";
    foreach (int itemId in ConnectedCharacters.NPCs[npcId].Inventory) {
      message += itemId + "|";
    }

    message = message.Trim('|');
    Send(message, reliableChannel, cnnId);
  }

  private void AddWorldItem(int worldId, int itemId, float x, float y, float z, bool onStart = false) {
    GameObject item = new GameObject();
    item.AddComponent<ItemReference>();
    item.GetComponent<ItemReference>().WorldId = worldId;
    item.GetComponent<ItemReference>().ItemId = itemId;
    item.transform.position = new Vector3(x, y, z);
    ItemList.WorldItems[worldId] = item;
    if (!onStart) {
      float[] args = {worldId, itemId, x, y, z};
      StartCoroutine("AddWorldItemCoroutine", args);
    }
  }

  private void DeleteWorldItem(int worldId) {
    ItemList.WorldItems.Remove(worldId);
    StartCoroutine("DeleteWorldItemCoroutine", worldId);
  }

  private void AddNPC(int npcId, string npcName, float px, float py, float pz, float rx, float ry, float rz,
    string type, int dialogueId) {
    NPC npc = new NPC();
    npc.Name = npcName;
    npc.Avatar.transform.position = new Vector3(px, py, pz);
    npc.Avatar.transform.rotation = Quaternion.Euler(new Vector3(rx, ry, rz));
    npc.Type = type;
    npc.DialogueId = dialogueId;
    ConnectedCharacters.NPCs.Add(npcId, npc);
    StartCoroutine("GetItemsForNPC", npcId);
  }

  private void Send(string message, int channelId, int cnnId) {
    List<int> connectionIds = new List<int>();
    connectionIds.Add(cnnId);
    Send(message, channelId, connectionIds);
  }

  private void Send(string message, int channelId, List<int> connectionIds) {
    Debug.Log("Sending : " + message);
    byte[] msg = Encoding.Unicode.GetBytes(message);
    foreach (int connectionId in connectionIds) {
      NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length * sizeof(char), out error);
    }
  }
}