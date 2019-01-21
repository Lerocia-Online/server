using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient {
  public int connectionId;
  public string playerName;
  public Vector3 position;
  public Quaternion rotation;
  public float moveTime;
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

  private void Start() {
    NetworkTransport.Init();
    ConnectionConfig cc = new ConnectionConfig();

    reliableChannel = cc.AddChannel(QosType.Reliable);
    unreliableChannel = cc.AddChannel(QosType.Unreliable);

    HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

    hostId = NetworkTransport.AddHost(topo, NetworkSettings.PORT, null);

    isStarted = true;
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
            OnNameIs(connectionId, splitData[1]);
            break;
          case "MYPOSITION":
            OnMyPosition(connectionId, float.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]),
              float.Parse(splitData[4]), float.Parse(splitData[5]), float.Parse(splitData[6]),
              float.Parse(splitData[7]), float.Parse(splitData[8]));
            break;
          case "ATK":
            OnAttack(connectionId);
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
  }

  private void OnDisconnection(int cnnId) {
    // Remove this player from our client list
    clients.Remove(clients.Find(x => x.connectionId == cnnId));

    // Tell everyone that somebody else has disconnected
    Send("DC|" + cnnId, reliableChannel, clients);
  }

  private void OnNameIs(int cnnId, string playerName) {
    // Link the name to the connection Id
    clients.Find(x => x.connectionId == cnnId).playerName = playerName;

    // Tell everybody that a new player has connected
    Send("CNN|" + playerName + '|' + cnnId, reliableChannel, clients);
  }

  private void OnMyPosition(int cnnId, float x, float y, float z, float rw, float rx, float ry, float rz, float time) {
    clients.Find(c => c.connectionId == cnnId).position = new Vector3(x, y, z);
    clients.Find(c => c.connectionId == cnnId).rotation = new Quaternion(rx, ry, rz, rw);
    clients.Find(c => c.connectionId == cnnId).moveTime = time;
  }
  
  private void OnAttack(int cnnId) {	
    string msg = "ATK|" + cnnId;
    Send(msg, reliableChannel, clients);
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