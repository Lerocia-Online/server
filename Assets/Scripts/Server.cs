using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Linq;

public class ServerClient {
  public int connectionId;
  public int userId;
  public string playerName;
  public Vector3 position;
  public Quaternion rotation;
  public float moveTime;
  public List<InventoryItem> inventory;
}

public class WorldItem {
  public int world_id;
  public int itemId;
  public Vector3 position;
}

public class InventoryItem {
  public int itemId;
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

public class Server : MonoBehaviour {
  private const int MAX_CONNECTION = 100;

  private int port = NetworkSettings.PORT;

  private int hostId;

  private int reliableChannel;
  private int unreliableChannel;

  private bool isStarted = false;
  private byte error;

  private List<ServerClient> clients = new List<ServerClient>();

  private float lastMovementUpdate;
  private float movementUpdateRate = 0.05f;
  
  private List<WorldItem> worldItems = new List<WorldItem>();

  private WWWForm form;
  private string getWorldItemsEndpoint = "get_world_items.php";
  private string getItemsForUserEndpoint = "get_items_for_user.php";
  private string addItemForUserEndpoint = "add_item_for_user.php";
  private string deleteItemForUserEndpoint = "delete_item_for_user.php";
  private string addWorldItemEndpoint = "add_world_item.php";
  private string deleteWorldItemEndpoint = "delete_world_item.php";

  private void Start() {
    NetworkTransport.Init();
    ConnectionConfig cc = new ConnectionConfig();

    reliableChannel = cc.AddChannel(QosType.Reliable);
    unreliableChannel = cc.AddChannel(QosType.Unreliable);

    HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

    hostId = NetworkTransport.AddHost(topo, NetworkSettings.PORT, null);

    isStarted = true;
    StartCoroutine("GetWorldItems");
  }

  private IEnumerator GetWorldItems() {
    form = new WWWForm();

    WWW w = new WWW(NetworkSettings.API + getWorldItemsEndpoint, form);
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

    WWW w = new WWW(NetworkSettings.API + getItemsForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      string jsonString = JsonHelper.fixJson(w.text);
      DatabaseItem[] dbi = JsonHelper.FromJson<DatabaseItem>(jsonString);
      clients.Find(x => x.connectionId == cnnId).inventory = new List<InventoryItem>();
      foreach (DatabaseItem it in dbi) {
        InventoryItem item = new InventoryItem();
        item.itemId = it.item_id;
        clients.Find(x => x.connectionId == cnnId).inventory.Add(item);
      }
      string inventoryMessage = "INVENTORY|";
      foreach (InventoryItem item in clients.Find(x => x.connectionId == cnnId).inventory) {
        inventoryMessage += item.itemId + "|";
      }
      inventoryMessage = inventoryMessage.Trim('|');
    
      // ITEMS|0%3%10.12%42.11%4.82|1%2%10.12%42.11%4.82|2%3%10.12%42.11%4.82
      Send(inventoryMessage, reliableChannel, cnnId);
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

    WWW w = new WWW(NetworkSettings.API + addItemForUserEndpoint, form);
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

    WWW w = new WWW(NetworkSettings.API + deleteItemForUserEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, deleted successfully
    } else {
      Debug.Log(w.error);
    }
  }
  
  private IEnumerator AddWorldItemCoroutine(float[] args) {
    form = new WWWForm();
    int worldId = (int)args[0];
    int itemId = (int)args[1];
    float x = args[2];
    float y = args[3];
    float z = args[4];

    form.AddField("world_id", worldId);
    form.AddField("item_id", itemId);
    form.AddField("position_x", x.ToString());
    form.AddField("position_y", y.ToString());
    form.AddField("position_z", z.ToString());

    WWW w = new WWW(NetworkSettings.API + addWorldItemEndpoint, form);
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

    WWW w = new WWW(NetworkSettings.API + deleteWorldItemEndpoint, form);
    yield return w;

    if (string.IsNullOrEmpty(w.error)) {
      // Do nothing, deleted successfully
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
          case "CHARGE":
            OnCharge(connectionId);
            break;
          case "ATK":
            OnAttack(connectionId);
            break;
          case "HIT":
            OnHit(connectionId, int.Parse(splitData[1]), int.Parse(splitData[2]));
            break;
          case "USE":
            OnUse(connectionId, int.Parse(splitData[1]));
            break;
          case "DROP":
            OnDrop(connectionId, int.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]), float.Parse(splitData[4]));
            break;
          case "PICKUP":
            OnPickup(connectionId, int.Parse(splitData[1]));
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
      foreach (ServerClient sc in clients) {
        m += sc.connectionId.ToString() + '%' + sc.position.x.ToString() + '%' + sc.position.y.ToString() + '%' +
             sc.position.z.ToString() + '%' + sc.rotation.w.ToString() + '%' + sc.rotation.x.ToString() + '%' +
             sc.rotation.y.ToString() + '%' + sc.rotation.z.ToString() + '%' + sc.moveTime.ToString() + '|';
      }

      m = m.Trim('|');
      Send(m, unreliableChannel, clients);
    }
  }

  private void OnConnection(int cnnId) {
    // Add him to a list
    ServerClient c = new ServerClient();
    c.connectionId = cnnId;
    c.playerName = "TEMP";
    clients.Add(c);

    // When the player joins the server, tell him his ID
    // Request his name and send the name of all the other players
    string msg = "ASKNAME|" + cnnId + "|";
    foreach (ServerClient sc in clients) {
      msg += sc.playerName + "%" + sc.connectionId + "|";
    }
    
    msg = msg.Trim('|');

    // ASKNAME|3|DAVE%1|MICHAEL%2|TEMP%3
    Send(msg, reliableChannel, cnnId);

    // Send all items placed in the world
    string itemsMessage = "ITEMS|";
    foreach (WorldItem item in worldItems) {
      itemsMessage += item.world_id + "%" + item.itemId + "%" + item.position.x + "%" + item.position.y +
                      "%" + item.position.z + "|";
    }

    itemsMessage = itemsMessage.Trim('|');
    
    // ITEMS|0%3%10.12%42.11%4.82|1%2%10.12%42.11%4.82|2%3%10.12%42.11%4.82
    Send(itemsMessage, reliableChannel, cnnId);
  }

  private void OnDisconnection(int cnnId) {
    // Remove this player from our client list
    clients.Remove(clients.Find(x => x.connectionId == cnnId));

    // Tell everyone that somebody else has disconnected
    Send("DC|" + cnnId, reliableChannel, clients);
  }

  private void OnNameIs(int cnnId, string playerName, int userId) {
    // Link the name to the connection Id
    clients.Find(x => x.connectionId == cnnId).playerName = playerName;
    clients.Find(x => x.connectionId == cnnId).userId = userId;
    int[] ids = {userId, cnnId};
    StartCoroutine("GetItemsForUser", ids);

    // Tell everybody that a new player has connected
    Send("CNN|" + playerName + '|' + cnnId, reliableChannel, clients);
  }

  private void OnMyPosition(int cnnId, float x, float y, float z, float rw, float rx, float ry, float rz, float time) {
    clients.Find(c => c.connectionId == cnnId).position = new Vector3(x, y, z);
    clients.Find(c => c.connectionId == cnnId).rotation = new Quaternion(rx, ry, rz, rw);
    clients.Find(c => c.connectionId == cnnId).moveTime = time;
  }

  private void OnCharge(int cnnId) {
    string msg = "CHARGE|" + cnnId;
    Send(msg, reliableChannel, clients);
  }
  
  private void OnAttack(int cnnId) {	
    string msg = "ATK|" + cnnId;
    Send(msg, reliableChannel, clients);
  }
  
  private void OnHit(int cnnId, int hitId, int damage) {	
    string msg = "HIT|" + cnnId + "|" + hitId + "|" + damage;
    Send(msg, reliableChannel, clients);
  }
  
  private void OnUse(int cnnId, int itemId) {
    int[] args = {cnnId, itemId};
    StartCoroutine("DeleteItemForUser", args);
    string msg = "USE|" + cnnId + "|" + itemId;
    Send(msg, reliableChannel, clients);
  }
  
  private void OnDrop(int cnnId, int itemId, float x, float y, float z) {
    int[] args = {clients.Find(c => c.connectionId == cnnId).userId, itemId};
    StartCoroutine("DeleteItemForUser", args);
    int worldId;
    if (worldItems.Count > 0) {
      worldId = worldItems[worldItems.Count - 1].world_id + 1;
    } else {
      worldId = 0;
    }
    AddWorldItem(worldId, itemId, x, y, z);
    string msg = "DROP|" + cnnId + "|" + worldId + "|" + itemId + "|" + x + "|" + y + "|" + z;
    Send(msg, reliableChannel, clients);
  }

  private void OnPickup(int cnnId, int worldId) {
    InventoryItem item = new InventoryItem();
    int[] args = {clients.Find(c => c.connectionId == cnnId).userId, worldItems.Find(i => i.world_id == worldId).itemId};
    StartCoroutine("AddItemForUser", args);
    item.itemId = worldItems.Find(i => i.world_id == worldId).itemId;
    clients.Find(c => c.connectionId == cnnId).inventory.Add(item);
    DeleteWorldItem(worldId);
    string msg = "PICKUP|" + cnnId + "|" + worldId;
    Send(msg, reliableChannel, clients);
  }

  private void AddWorldItem(int worldId, int itemId, float x, float y, float z, bool onStart = false) {
    WorldItem item = new WorldItem();
    item.world_id = worldId;
    item.itemId = itemId;
    item.position = new Vector3(x, y, z);
    worldItems.Add(item);
    if (!onStart) {
      float[] args = {worldId, itemId, x, y, z};
      StartCoroutine("AddWorldItemCoroutine", args);
    }
  }

  private void DeleteWorldItem(int worldId) {
    worldItems.Remove(worldItems.Find(i => i.world_id == worldId));
    StartCoroutine("DeleteWorldItemCoroutine", worldId);
  }

  private void Send(string message, int channelId, int cnnId) {
    List<ServerClient> c = new List<ServerClient>();
    c.Add(clients.Find(x => x.connectionId == cnnId));
    Send(message, channelId, c);
  }

  private void Send(string message, int channelId, List<ServerClient> c) {
    Debug.Log("Sending : " + message);
    byte[] msg = Encoding.Unicode.GetBytes(message);
    foreach (ServerClient sc in c) {
      NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, message.Length * sizeof(char), out error);
    }
  }
}