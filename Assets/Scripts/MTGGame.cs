using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;
using Sfs2X.Requests.Game;


public class MTGGame: MonoBehaviour {
	private SmartFox sfs;
	private Dictionary<int, Player> players;
	public Transform prefab;
	

	private const string extensionId = "MTG";
	private const string extensionClass = "ca.rivalstudios.mtg.MTGExtension";
	
	void Awake() {
		Application.runInBackground = true;
		Security.PrefetchSocketPolicy("127.0.0.1", 9933);
		
		if (SmartFoxConnection.IsInitialized) {
			sfs = SmartFoxConnection.Connection;
		} else {
			Application.LoadLevel("login");
			return;
		}
		
		StartGame();
		
			SFSGameSettings settings = new SFSGameSettings(sfs.MySelf.Name + "'s game");
			settings.IsPublic = true;
			settings.MaxUsers = 2;
			settings.MaxSpectators = 0;
			settings.MinPlayersToStartGame = 1;
			settings.Extension = new RoomExtension(extensionId, extensionClass);
			settings.LeaveLastJoinedRoom = true;
			sfs.Send( new CreateSFSGameRequest(settings));
		
		//StartGame();
	}
	
	void Update() {
		RaycastHit hit = new RaycastHit();
		if (Input.GetButtonDown("Fire1")) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out hit)) {
				if (hit.transform.tag.Equals("Ground")) {
					PlayerMove(hit.point.x, hit.point.y, hit.point.z);
				}
			}
		}		
	}
		
	void FixedUpdate() {
		// Required to run on the clients to ensure thread safety
		sfs.ProcessEvents();
	}
	
	public void StartGame() {
		Debug.Log("StartGame()");
		players = new Dictionary<int, Player>();
		
		sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);		
		//sfs.Send(new ExtensionRequest(Commands.READY, new SFSObject(), sfs.LastJoinedRoom));
	}
	
	public void DestroyGame() {
		sfs.RemoveEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
	}
	
	public void PlayerMove(float x, float y, float z) {
		//Debug.Log("PlayerMove: " + x + " " + y + " " + z);
		ISFSObject obj = new SFSObject();
		obj.PutFloat("x", x);
		obj.PutFloat("y", y);
		obj.PutFloat("z", z);
	
		sfs.Send(new ExtensionRequest(Commands.MOVE, obj, sfs.LastJoinedRoom));
	}
	
	public void MoveReceived(int id, float x, float y, float z) {
		if (players.ContainsKey(id)) {
			players[id].setPosition(new Vector3(x, y, z));
		}
	}
	
	public void SpawnPlayer(int id, float x, float y, float z) {
		Player newPlayer = new Player(1, (Transform)Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity));
		players.Add(id, newPlayer);
	}
	
	public void OnExtensionResponse(BaseEvent evt) {
		string cmd = (string)evt.Params["cmd"];
		SFSObject dataObject = (SFSObject)evt.Params["params"];
		
		switch (cmd) {
		case Commands.START:
			StartGame();
			break;
		case Commands.MOVE:
			Debug.Log("ServerMove: " + dataObject.GetInt("i") + " " + dataObject.GetFloat("x") + " " +  dataObject.GetFloat("y") + " " + dataObject.GetFloat("z"));
			MoveReceived(dataObject.GetInt("i"), dataObject.GetFloat("x"), dataObject.GetFloat("y"), dataObject.GetFloat("z"));
			break;
		case Commands.WIN:
			break;
		case Commands.SPAWN:
			Debug.Log("Spawn: " + dataObject.GetInt("i") + " " + dataObject.GetFloat("x") + " " +  dataObject.GetFloat("y") + " " + dataObject.GetFloat("z"));
			SpawnPlayer(dataObject.GetInt("i"), dataObject.GetFloat("x"),  dataObject.GetFloat("y"), dataObject.GetFloat("z"));
			break;
		case Commands.HEALTH:
			Debug.Log("Health: " + dataObject.GetFloat("h"));
			break;
		}
	}
}
