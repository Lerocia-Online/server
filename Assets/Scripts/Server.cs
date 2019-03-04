using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.ComponentModel;
using Lerocia.Characters;
using Lerocia.Characters.NPCs;
using Lerocia.Characters.Players;
using Lerocia.Items;
using Networking.Constants;
using UnityEngine.AI;
using Random = System.Random;

class ServerPlayer : Player {
  private Server _myServer;

  public ServerPlayer() {
    Inventory.BeforeRemove += RequestItemDeletion;
    _myServer = GameObject.Find("Server").GetComponent<Server>();
  }

  private void RequestItemDeletion(int deletedItem) {
    int[] args = {CharacterId, deletedItem};
    _myServer.StartCoroutine("DeleteItemForPlayer", args);
  }

  public override void InitializeOnInventoryChange() {
    Inventory.ListChanged += OnInventoryChange;
  }

  protected override void OnInventoryChange(object sender, ListChangedEventArgs e) {
    if (e.ListChangedType == ListChangedType.ItemAdded) {
      int[] args = {CharacterId, Inventory[e.NewIndex]};
      _myServer.StartCoroutine("AddItemForPlayer", args);
    }
  }
}

[Serializable]
class DatabasePlayer {
  public bool success;
  public string error;
  public int character_id;
  public string character_name;
  public string character_personality;
  public float position_x;
  public float position_y;
  public float position_z;
  public float rotation_x;
  public float rotation_y;
  public float rotation_z;
  public int max_health;
  public int current_health;
  public int max_stamina;
  public int current_stamina;
  public int gold;
  public int base_weight;
  public int base_damage;
  public int base_armor;
  public int weapon_id;
  public int apparel_id;
  public int dialogue_id;
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
public class DatabaseDestination {
  public float position_x;
  public float position_y;
  public float position_z;
  public float duration;
}

[Serializable]
class DatabaseNPC {
  public int character_id;
  public int npc_id;
  public string character_name;
  public string character_personality;
  public float position_x;
  public float position_y;
  public float position_z;
  public float rotation_x;
  public float rotation_y;
  public float rotation_z;
  public int max_health;
  public int current_health;
  public int max_stamina;
  public int current_stamina;
  public int gold;
  public int base_weight;
  public int base_damage;
  public int base_armor;
  public int weapon_id;
  public int apparel_id;
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
  private string getItemsForCharacterEndpoint = "get_items_for_character.php";
  private string getStatsForCharacterEndpoint = "get_stats_for_character.php";
  private string setStatsForCharacterEndpoint = "set_stats_for_character.php";
  private string addItemForCharacterEndpoint = "add_item_for_character.php";
  private string deleteItemForCharacterEndpoint = "delete_item_for_character.php";
  private string addWorldItemEndpoint = "add_world_item.php";
  private string deleteWorldItemEndpoint = "delete_world_item.php";
  private string getDestinationsForNpcEndpoint = "get_destinations_for_npc.php";
  private string logoutEndpoint = "logout.php";
  private string logoutAllPlayersEndpoint = "logout_all_players.php";

  [SerializeField]
  private GameObject _playerPrefab;
  [SerializeField]
  private GameObject _npcPrefab;
  [SerializeField]
  private GameObject _itemPrefab;

  private void Awake() {
    StartCoroutine("LogoutAllPlayers");
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
        AddNPC(
          npc.character_id, 
          npc.character_name, 
          npc.character_personality, 
          npc.position_x, npc.position_y, npc.position_z, 
          npc.rotation_x, npc.rotation_y, npc.rotation_z, 
          npc.max_health, npc.current_health,
          npc.max_stamina, npc.current_stamina,
          npc.gold,
          npc.base_weight,
          npc.base_damage,
          npc.base_armor,
          npc.weapon_id,
          npc.apparel_id,
          npc.dialogue_id
        );
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

  private IEnumerator GetItemsForPlayer(int characterId) {
    form = new WWWForm();

    form.AddField("character_id", characterId);

    WWW w = new WWW(NetworkConstants.Api + getItemsForCharacterEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseItem[] dbi = JsonHelper.FromJson<DatabaseItem>(jsonString);
      foreach (DatabaseItem it in dbi) {
        ConnectedCharacters.Characters[characterId].Inventory.Add(it.item_id);
      }

      ConnectedCharacters.Characters[characterId].InitializeOnInventoryChange();

      string inventoryMessage = "INVENTORY|";
      foreach (int itemId in ConnectedCharacters.Characters[characterId].Inventory) {
        inventoryMessage += itemId + "|";
      }

      inventoryMessage = inventoryMessage.Trim('|');

      Send(inventoryMessage, reliableChannel, ConnectedCharacters.ConnectionIds);
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetStatsForPlayer(int characterId) {
    form = new WWWForm();

    form.AddField("character_id", characterId);

    WWW w = new WWW(NetworkConstants.Api + getStatsForCharacterEndpoint, form);
    yield return w;
    Debug.Log(w.text);
    if (string.IsNullOrEmpty(w.error)) {
      DatabasePlayer dbp = JsonUtility.FromJson<DatabasePlayer>(w.text);
      ConnectedCharacters.Players[characterId].Avatar.transform.position =
        new Vector3(dbp.position_x, dbp.position_y, dbp.position_z);
      ConnectedCharacters.Players[characterId].Avatar.transform.rotation =
        Quaternion.Euler(new Vector3(dbp.rotation_x, dbp.rotation_y, dbp.rotation_z));
      ConnectedCharacters.Players[characterId].CharacterPersonality = dbp.character_personality;
      ConnectedCharacters.Players[characterId].MaxHealth = dbp.max_health;
      ConnectedCharacters.Players[characterId].CurrentHealth = dbp.current_health;
      ConnectedCharacters.Players[characterId].MaxStamina = dbp.max_stamina;
      ConnectedCharacters.Players[characterId].CurrentStamina = dbp.current_stamina;
      ConnectedCharacters.Players[characterId].Gold = dbp.gold;
      ConnectedCharacters.Players[characterId].BaseWeight = dbp.base_weight;
      ConnectedCharacters.Players[characterId].BaseDamage = dbp.base_damage;
      ConnectedCharacters.Players[characterId].BaseArmor = dbp.base_armor;
      ConnectedCharacters.Players[characterId].WeaponId = dbp.weapon_id;
      ConnectedCharacters.Players[characterId].ApparelId = dbp.apparel_id;
      ConnectedCharacters.Players[characterId].DialogueId = dbp.dialogue_id;
      ConnectedCharacters.Players[characterId].Dialogues = DialogueList.Dialogues[dbp.dialogue_id];

      // Tell everybody that a new player has connected
      Send(
        "CNN|" + 
        characterId + '|' + 
        dbp.character_name + '|' + 
        dbp.character_personality + '|' + 
        dbp.position_x + '|' + 
        dbp.position_y + '|' + 
        dbp.position_z + '|' +
        dbp.rotation_x + '|' + 
        dbp.rotation_y + '|' + 
        dbp.rotation_z + '|' + 
        dbp.max_health + '|' + 
        dbp.current_health + '|' + 
        dbp.max_stamina + '|' +
        dbp.current_stamina + '|' + 
        dbp.gold + '|' +
        dbp.base_weight + '|' +
        dbp.base_damage + '|' +
        dbp.base_armor + '|' + 
        dbp.weapon_id + '|' + 
        dbp.apparel_id + '|' +
        dbp.dialogue_id,
        reliableChannel, ConnectedCharacters.ConnectionIds
      );
      StartCoroutine("GetItemsForPlayer", characterId);
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator SetStatsForPlayer(Player player) {
    form = new WWWForm();
    form.AddField("character_id", player.CharacterId);
    form.AddField("character_personality", player.CharacterPersonality);
    form.AddField("position_x", player.Avatar.transform.position.x.ToString());
    form.AddField("position_y", player.Avatar.transform.position.y.ToString());
    form.AddField("position_z", player.Avatar.transform.position.z.ToString());
    form.AddField("rotation_x", player.Avatar.transform.rotation.eulerAngles.x.ToString());
    form.AddField("rotation_y", player.Avatar.transform.rotation.eulerAngles.y.ToString());
    form.AddField("rotation_z", player.Avatar.transform.rotation.eulerAngles.z.ToString());
    form.AddField("max_health", player.MaxHealth);
    form.AddField("current_health", player.CurrentHealth);
    form.AddField("max_stamina", player.MaxStamina);
    form.AddField("current_stamina", player.CurrentStamina);
    form.AddField("gold", player.Gold);
    form.AddField("base_weight", player.BaseWeight);
    form.AddField("base_damage", player.BaseDamage);
    form.AddField("base_armor", player.BaseArmor);
    form.AddField("weapon_id", player.WeaponId);
    form.AddField("apparel_id", player.ApparelId);
    form.AddField("dialogue_id", player.DialogueId);

    WWW w = new WWW(NetworkConstants.Api + setStatsForCharacterEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, logout successful
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator GetItemsForCharacter(int characterId) {
    form = new WWWForm();

    form.AddField("character_id", characterId);

    WWW w = new WWW(NetworkConstants.Api + getItemsForCharacterEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseItem[] dbi = JsonHelper.FromJson<DatabaseItem>(jsonString);
      foreach (DatabaseItem it in dbi) {
        ConnectedCharacters.Characters[characterId].Inventory.Add(it.item_id);
      }
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator AddItemForPlayer(int[] args) {
    Debug.Log("Calling add item for character");
    form = new WWWForm();
    int characterId = args[0];
    int itemId = args[1];

    form.AddField("character_id", characterId);
    form.AddField("item_id", itemId);

    WWW w = new WWW(NetworkConstants.Api + addItemForCharacterEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, added successfully
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator DeleteItemForPlayer(int[] args) {
    Debug.Log("Calling delete item for character");
    form = new WWWForm();
    int characterId = args[0];
    int itemId = args[1];

    form.AddField("character_id", characterId);
    form.AddField("item_id", itemId);

    WWW w = new WWW(NetworkConstants.Api + deleteItemForCharacterEndpoint, form);
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

  private IEnumerator GetDestinationsForNPC(int characterId) {
    form = new WWWForm();
    form.AddField("character_id", characterId);
    
    WWW w = new WWW(NetworkConstants.Api + getDestinationsForNpcEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseDestination[] dbd = JsonHelper.FromJson<DatabaseDestination>(jsonString);
      foreach (DatabaseDestination d in dbd) {
        ConnectedCharacters.NPCs[characterId].Destinations.Add(new Destination(new Vector3(d.position_x, d.position_y, d.position_z), d.duration));
      }
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator Logout(int characterId) {
    form = new WWWForm();
    form.AddField("character_id", characterId);

    WWW w = new WWW(NetworkConstants.Api + logoutEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, logout successful
    } else {
      Debug.Log(w.error);
    }
  }

  private IEnumerator LogoutAllPlayers() {
    form = new WWWForm();

    WWW w = new WWW(NetworkConstants.Api + logoutAllPlayersEndpoint, form);
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
    int characterId;
    ConnectedCharacters.IdMap.TryGetByFirst(connectionId, out characterId);
    switch (recData) {
      case NetworkEventType.ConnectEvent:
        OnConnection(connectionId);
        break;
      case NetworkEventType.DataEvent:
        string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
        Debug.Log("Receiving: " + msg);
        string[] splitData = msg.Split('|');
        switch (splitData[0]) {
          case "NAMEIS":
            OnNameIs(connectionId, splitData[1], int.Parse(splitData[2]));
            break;
          case "MYPOSITION":
            OnMyPosition(characterId, float.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]),
              float.Parse(splitData[4]), float.Parse(splitData[5]), float.Parse(splitData[6]),
              float.Parse(splitData[7]), float.Parse(splitData[8]));
            break;
          case "ATK":
            OnAttack(characterId);
            break;
          case "HIT":
            OnHit(characterId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          case "HITNPC":
            OnHitNPC(characterId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          case "USE":
            OnUse(characterId, int.Parse(splitData[1]));
            break;
          case "DROP":
            OnDrop(characterId, int.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]),
              float.Parse(splitData[4]));
            break;
          case "PICKUP":
            OnPickup(characterId, int.Parse(splitData[1]));
            break;
          case "NPCITEMS":
            OnNPCItems(characterId, int.Parse(splitData[1]));
            break;
          case "BUY":
            OnBuy(characterId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          default:
            Debug.Log("Invalid message : " + msg);
            break;
        }

        break;
      case NetworkEventType.DisconnectEvent:
        OnDisconnection(characterId);
        break;
    }

    // Ask player for their position
    if (Time.time - lastMovementUpdate > movementUpdateRate) {
      lastMovementUpdate = Time.time;
      string m = "ASKPOSITION|";
      foreach (NPC npc in ConnectedCharacters.NPCs.Values) {
        npc.MoveTime = Time.time - npc.TimeBetweenMovementStart;
      }
      foreach (int charId in ConnectedCharacters.Characters.Keys) {
        Character character = ConnectedCharacters.Characters[charId];
        m += charId.ToString() + '%' + 
             character.Avatar.transform.position.x + '%' +
             character.Avatar.transform.position.y + '%' +
             character.Avatar.transform.position.z + '%' + 
             character.Avatar.transform.rotation.w + '%' +
             character.Avatar.transform.rotation.x + '%' +
             character.Avatar.transform.rotation.y + '%' + 
             character.Avatar.transform.rotation.z + '%' +
             character.MoveTime + '|';
      }

      m = m.Trim('|');
      Send(m, unreliableChannel, ConnectedCharacters.ConnectionIds);
      foreach (NPC npc in ConnectedCharacters.NPCs.Values) {
        npc.TimeBetweenMovementStart = Time.time;
      }
    }
  }

  private void OnConnection(int connectionId) {
    ConnectedCharacters.ConnectionIds.Add(connectionId);
    // When the player joins the server, tell him his ID
    // Request his name and send the name of all the other players
    string msg = "ASKNAME|" + connectionId + "|";
    foreach (int characterId in ConnectedCharacters.Players.Keys) {
      Player p = ConnectedCharacters.Players[characterId];
      msg += 
        characterId + "%" + 
        p.CharacterName + '%' + 
        p.CharacterPersonality + "%" + 
        p.Avatar.transform.position.x + '%' + 
        p.Avatar.transform.position.y +'%' + 
        p.Avatar.transform.position.z + '%' + 
        p.Avatar.transform.rotation.eulerAngles.x + '%' +
        p.Avatar.transform.rotation.eulerAngles.y + '%' + 
        p.Avatar.transform.rotation.eulerAngles.z + '%' +
        p.MaxHealth + '%' + 
        p.CurrentHealth + '%' +
        p.MaxStamina + '%' + 
        p.CurrentStamina + '%' + 
        p.Gold + "%" +
        p.BaseWeight + "%" +
        p.BaseArmor + "%" +
        p.BaseDamage + "%" +
        p.WeaponId + '%' + 
        p.ApparelId + '%' + 
        p.DialogueId + "|";
    }

    msg = msg.Trim('|');

    // ASKNAME|3|DAVE%1|MICHAEL%2|TEMP%3
    Send(msg, reliableChannel, connectionId);

    // Send all items placed in the world
    string itemsMessage = "ITEMS|";
    foreach (GameObject item in ItemList.WorldItems.Values) {
      itemsMessage += item.GetComponent<ItemReference>().WorldId + "%" + item.GetComponent<ItemReference>().ItemId +
                      "%" + item.transform.position.x + "%" + item.transform.position.y + "%" +
                      item.transform.position.z + "|";
    }

    itemsMessage = itemsMessage.Trim('|');

    // ITEMS|0%3%10.12%42.11%4.82|1%2%10.12%42.11%4.82|2%3%10.12%42.11%4.82
    Send(itemsMessage, reliableChannel, connectionId);

    // Send all NPCs in the world
    string npcsMessage = "NPCS|";
    foreach (int characterId in ConnectedCharacters.NPCs.Keys) {
      NPC npc = ConnectedCharacters.NPCs[characterId];
      npcsMessage += 
        characterId + "%" + 
        npc.CharacterName + "%" + 
        npc.CharacterPersonality + "%" + 
        npc.Avatar.transform.position.x + "%" +
        npc.Avatar.transform.position.y + "%" +
        npc.Avatar.transform.position.z + "%" + 
        npc.Avatar.transform.rotation.eulerAngles.x + "%" +
        npc.Avatar.transform.rotation.eulerAngles.y + "%" +
        npc.Avatar.transform.rotation.eulerAngles.z + "%" + 
        npc.MaxHealth + '%' + 
        npc.CurrentHealth + '%' +
        npc.MaxStamina + '%' + 
        npc.CurrentStamina + '%' + 
        npc.Gold + "%" +
        npc.BaseWeight + "%" +
        npc.BaseArmor + "%" +
        npc.BaseDamage + "%" +
        npc.WeaponId + '%' + 
        npc.ApparelId + '%' + 
        npc.DialogueId + "|";
    }

    npcsMessage = npcsMessage.Trim('|');

    // NPCS|0%Harold|1%Johnny|2%Michelle
    Send(npcsMessage, reliableChannel, connectionId);
  }

  private void OnDisconnection(int characterId) {
    // Update players transform
    StartCoroutine("SetStatsForPlayer", ConnectedCharacters.Players[characterId]);
    // Logout player
    StartCoroutine("Logout", characterId);

    // Remove this player from our client list
    ConnectedCharacters.Players.Remove(characterId);
    ConnectedCharacters.Characters.Remove(characterId);
    int connectionId;
    ConnectedCharacters.IdMap.TryGetBySecond(characterId, out connectionId);
    ConnectedCharacters.IdMap.RemoveByFirst(connectionId);
    ConnectedCharacters.ConnectionIds.Remove(connectionId);

    // Tell everyone that somebody else has disconnected
    Send("DC|" + characterId, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnNameIs(int cnnId, string name, int characterId) {
    ConnectedCharacters.IdMap.Add(cnnId, characterId);
    // Link the name to the connection Id
    Player player = new ServerPlayer();
    player.CharacterName = name;
    player.CharacterId = characterId;
    player.Avatar = Instantiate(_playerPrefab);
    player.Avatar.name = name;
    ConnectedCharacters.Players.Add(characterId, player);
    ConnectedCharacters.Characters.Add(characterId, player);
    
    StartCoroutine("GetStatsForPlayer", characterId);
  }

  private void OnMyPosition(int characterId, float x, float y, float z, float rw, float rx, float ry, float rz, float time) {
    ConnectedCharacters.Players[characterId].Avatar.transform.position = new Vector3(x, y, z);
    ConnectedCharacters.Players[characterId].Avatar.transform.rotation = new Quaternion(rx, ry, rz, rw);
    ConnectedCharacters.Players[characterId].MoveTime = time;
  }

  private void OnAttack(int characterId) {
    string msg = "ATK|" + characterId;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnHit(int characterId, int hitId, int damage) {
    //TODO Remove health from client on server
    string msg = "HIT|" + characterId + "|" + hitId + "|" + damage;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnHitNPC(int characterId, int hitId, int damage) {
    //TODO Remove health from NPC on server
    string msg = "HITNPC|" + characterId + "|" + hitId + "|" + damage;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnUse(int characterId, int itemId) {
    //TODO Fix this method to 'Use' items properly...
    ItemList.Items[itemId].Use(ConnectedCharacters.Players[characterId]);
    string msg = "USE|" + characterId + "|" + itemId;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnDrop(int characterId, int itemId, float x, float y, float z) {
    ConnectedCharacters.Players[characterId].Inventory.Remove(itemId);
    Random random = new Random();
    int worldId = random.Next();

    AddWorldItem(worldId, itemId, x, y, z);
    string msg = "DROP|" + characterId + "|" + worldId + "|" + itemId + "|" + x + "|" + y + "|" + z;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnPickup(int characterId, int worldId) {
    ConnectedCharacters.Players[characterId].Inventory.Add(ItemList.WorldItems[worldId].GetComponent<ItemReference>().ItemId);
    DeleteWorldItem(worldId);
    string msg = "PICKUP|" + characterId + "|" + worldId;
    Send(msg, reliableChannel, ConnectedCharacters.ConnectionIds);
  }

  private void OnNPCItems(int characterId, int npcId) {
    string message = "NPCITEMS|" + npcId + "|";
    foreach (int itemId in ConnectedCharacters.NPCs[npcId].Inventory) {
      message += itemId + "|";
    }

    message = message.Trim('|');
    int connectionId;
    ConnectedCharacters.IdMap.TryGetBySecond(characterId, out connectionId);
    Send(message, reliableChannel, connectionId);
  }
  
  private void OnBuy(int characterId, int merchantId, int itemId) {
    if (ConnectedCharacters.Characters[characterId].BuyItem(ConnectedCharacters.Characters[merchantId], itemId)) {
      //TODO This should be handled by an onDelete handler for NPC inventory (similar to Player)
      int[] args = {merchantId, itemId};
      StartCoroutine("DeleteItemForPlayer", args);
    } else {
      Debug.Log("Item purchase failed");
    }
  }

  private void AddWorldItem(int worldId, int itemId, float x, float y, float z, bool onStart = false) {
    GameObject item = Instantiate(_itemPrefab);
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

  private void AddNPC(
    int characterId, 
    string characterName, 
    string characterPersonality, 
    float px, float py, float pz, 
    float rx, float ry, float rz,
    int maxHealth, int currentHealth,
    int maxStamina, int currentStamina,
    int gold,
    int baseWeight,
    int baseDamage,
    int baseArmor,
    int weaponId,
    int apparelId,
    int dialogueId
  ) {
    GameObject npcObject = Instantiate(_npcPrefab);
    npcObject.name = characterName;
    npcObject.GetComponent<NavMeshAgent>().Warp(new Vector3(px, py, pz));
    npcObject.transform.rotation = Quaternion.Euler(new Vector3(rx, ry, rz));
    npcObject.AddComponent<NPCController>();
    if (characterPersonality == "friendly") {
      npcObject.GetComponent<NPCController>().TargetTypes = new List<string> {"enemy"};
    } else if (characterPersonality == "enemy") {
      npcObject.GetComponent<NPCController>().TargetTypes = new List<string> {"friendly", "passive"};
    } else if (characterPersonality == "passive") {
      // Do nothing, passive does not target
    } else {
      Debug.Log("Invalid personality");
    }
    npcObject.AddComponent<CharacterReference>();
    npcObject.GetComponent<CharacterReference>().CharacterId = characterId;
    NPC npc = new NPC(
      characterId,
      characterName,
      characterPersonality,
      npcObject,
      maxHealth,
      currentHealth,
      maxStamina,
      currentStamina,
      gold,
      baseWeight,
      baseDamage,
      baseArmor,
      weaponId,
      apparelId,
      dialogueId
    );

    npcObject.GetComponent<NPCController>().Npc = npc;
    ConnectedCharacters.NPCs.Add(characterId, npc);
    ConnectedCharacters.Characters.Add(characterId, npc);
    StartCoroutine("GetItemsForCharacter", characterId);
    StartCoroutine("GetDestinationsForNPC", characterId);
  }

  private void Send(string message, int channelId, int connectionId) {
    List<int> connectionIds = new List<int>();
    connectionIds.Add(connectionId);
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