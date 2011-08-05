using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Requests;
using Sfs2X.Logging;
using Sfs2X.Requests.Game;
using Sfs2X.Entities.Match;

public class LobbyGUI : MonoBehaviour {
	private SmartFox smartFox;
	private bool shuttingDown = false;

	private Vector2 gameScrollPosition, userScrollPosition;
	private ChatWindow chatWindow;

	private int roomSelection = -1;
	private string [] roomStrings;

	private const string extensionId = "MTG";
	private const string extensionClass = "ca.rivalstudios.mtg.MTGExtension";

	
	public GUISkin gSkin;
	
	private bool started = false;

	/************
	 * Unity callback methods
	 ************/

	void OnApplicationQuit() {
		shuttingDown = true;
	}
	
	void FixedUpdate() {
		smartFox.ProcessEvents();
	}

	void Awake() {
		Application.runInBackground = true;
		Security.PrefetchSocketPolicy("127.0.0.1", 9933);

		if ( SmartFoxConnection.IsInitialized ) {
			smartFox = SmartFoxConnection.Connection;
		} else {
			Application.LoadLevel("login");
			return;
		}

		// Register callbacks
		smartFox.AddEventListener(SFSEvent.LOGOUT, OnLogout);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		smartFox.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
		smartFox.AddEventListener(SFSEvent.ROOM_JOIN, OnJoinRoom);
		smartFox.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnCreateRoomError);
		smartFox.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
		smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
		smartFox.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
		smartFox.AddEventListener(SFSEvent.ROOM_REMOVE, OnRoomDeleted);
		
		chatWindow = new ChatWindow(20, 110, Screen.width-250, Screen.height-150);

		// Lets update internal API room list that might have changed while we played a game
		SetupRoomList();
		
		started = true;
	}

	void OnGUI() {

		if (!started) return;

		//GUI.skin = gSkin;
		int screenW = Screen.width;
		int screenH = Screen.height;

		Room currentActiveRoom = smartFox.LastJoinedRoom;

		if ( currentActiveRoom == null ) {
			// Wait until active room has been set up in the API before drawing anything
			return;
		}

		// User list
		GUI.Box(new Rect(screenW-200, 80, 180, 170), "Users");
		GUILayout.BeginArea(new Rect(screenW-190, 110, 150, 160));
		userScrollPosition = GUILayout.BeginScrollView(userScrollPosition, GUILayout.Width(150), GUILayout.Height(130));
		GUILayout.BeginVertical();

		foreach ( User user in currentActiveRoom.UserList ) {
			GUILayout.Label(user.Name);
		}
		
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
		GUILayout.EndArea();


		// Game room list
		GUI.Box(new Rect(screenW-200, 260, 180, 200), "Game List");
		if (smartFox.RoomList.Count != 1 ) {
			GUILayout.BeginArea(new Rect(screenW-190, 290, 150, 130));
			gameScrollPosition = GUILayout.BeginScrollView(gameScrollPosition, GUILayout.Width(150), GUILayout.Height(160));

			//roomSelection = GUILayout.SelectionGrid(roomSelection, roomStrings, 1, "RoomListButton");
			if (roomSelection>=0 && roomStrings[roomSelection] != currentActiveRoom.Name)
			{
				smartFox.Send(new JoinRoomRequest(roomStrings[roomSelection], null, smartFox.LastJoinedRoom.Id));
			}

			if ( smartFox.RoomList.Count == 1 ) { // We always have 1 non-game room - Main Lobby
				GUILayout.Label("No games available to join");
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		if ( GUI.Button(new Rect(screenW-200, 480, 85, 24), "New game") ) {
			SFSGameSettings settings = new SFSGameSettings("TestGame");
			settings.IsPublic = true;
			settings.MaxUsers = 2;
			settings.MaxSpectators = 0;
			settings.MinPlayersToStartGame = 1;
			settings.Extension = new RoomExtension(extensionId, extensionClass);
			settings.LeaveLastJoinedRoom = true;
			smartFox.Send( new CreateSFSGameRequest(settings));
			//smartFox.Send(new CreateRoomRequest(settings, true, smartFox.LastJoinedRoom));
			
/*		
			
			
			SFSGameSettings settings = new SFSGameSettings("TestGame");
			settings.IsPublic = true;
			settings.MaxUsers = 4;
			settings.MaxSpectators = 0;
			settings.MinPlayersToStartGame = 2;
			settings.PlayerMatchExpression = new MatchExpression("BestScore", NumberMatch.GREATER_THAN, 100);
			settings.NotifyGameStarted = true;
			settings.LeaveLastJoinedRoom = true;
			
			smartFox.Send( new CreateSFSGameRequest(settings) );		*/	
			
		}

		// Standard view
		if ( GUI.Button(new Rect(screenW-105, 480, 85, 24), "Logout") ) {
			smartFox.Send(new LogoutRequest());
		}

		// Room chat window
		GUI.Box(new Rect(10, 80, screenW-230, screenH-100), "Chat");
		chatWindow.Draw();

	}

	/************
	 * Helper methods
	 ************/

	private void UnregisterSFSSceneCallbacks() {
		// This should be called when switching scenes, so callbacks from the backend do not trigger code in this scene
		smartFox.RemoveAllEventListeners();
	}

	private void SetupRoomList() {	
		List<Room> roomList = smartFox.RoomManager.GetRoomList();
		List<string> roomNames = new List<string>();
		foreach (Room room in roomList) {
			// Show only game rooms
			if (!room.IsGame || room.IsHidden || room.IsPasswordProtected) {
				continue;
			}	
			
			roomNames.Add(room.Name);
			Debug.Log("Room id: " + room.Id + " has name: " + room.Name);
				
		}
		
		roomStrings = roomNames.ToArray();
		
		if (smartFox.LastJoinedRoom==null)
			smartFox.Send(new JoinRoomRequest("The Lobby"));
	}

	/************
	 * Callbacks from the SFS API
	 ************/

	void OnLogout(BaseEvent evt) {
		UnregisterSFSSceneCallbacks();
		Application.LoadLevel("login");
	}

	void OnConnectionLost(BaseEvent evt) {
		UnregisterSFSSceneCallbacks();
		if ( shuttingDown == true ) return;
		Application.LoadLevel("login");
	}

	void OnPublicMessage(BaseEvent evt) {
		string message = (string)evt.Params["message"];
		User sender = (User)evt.Params["sender"];
		chatWindow.AddChatMessage(sender.Name + " said " + message);
	}

	void OnJoinRoom(BaseEvent evt) {
		Room room = (Room)evt.Params["room"];
		// If we joined a game room, then we either created it (and auto joined) or manually selected a game to join
		if ( room.IsGame ) {
			started = false;
			Debug.Log("Joining game room " + room.Name);
			UnregisterSFSSceneCallbacks();
			Application.LoadLevel("game");
		}
	}

	public void OnCreateRoomError(BaseEvent evt) {
		string error = (string)evt.Params["errorMessage"];
		Debug.Log("Room creation error; the following error occurred: " + error);
	}

	public void OnUserEnterRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
		chatWindow.AddPlayerJoinMessage(user.Name + " joined room");
	}

	private void OnUserLeaveRoom(BaseEvent evt) {
		User user = (User)evt.Params["user"];
		chatWindow.AddPlayerLeftMessage(user.Name + " left room");
	}

	/*
* Handle a new room in the room list
*/
	public void OnRoomAdded(BaseEvent evt) { //Room room) {
		Room room = (Room)evt.Params["room"];
		// Update view (only if room is game)
		if ( room.IsGame ) {
			SetupRoomList();
		}
	}

	/*
	* Handle a room that was removed
	*/
	public void OnRoomDeleted(BaseEvent evt) { //Room room) {
		SetupRoomList();
	}

}