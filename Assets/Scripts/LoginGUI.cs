using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Requests;
using Sfs2X.Logging;

public class LoginGUI : MonoBehaviour {
	private SmartFox smartFox;
	private bool shuttingDown = false;

	public string serverIP = "127.0.0.1";
	public int serverPort = 9933;
	public string zone = "MTG";	
	public bool debug = true;

	public GUISkin gSkin;

	private string username = "";
	private string loginErrorMessage = "";

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

		if (SmartFoxConnection.IsInitialized)
		{
			smartFox = SmartFoxConnection.Connection;
		} else {
			smartFox = new SmartFox(debug);
		}

		// Register callback delegate
		smartFox.AddEventListener(SFSEvent.CONNECTION, OnConnection);
		smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
		smartFox.AddEventListener(SFSEvent.LOGIN, OnLogin);
		
		smartFox.AddLogListener(LogLevel.DEBUG, OnDebugMessage);

		smartFox.Connect(serverIP, serverPort);
	}

	void OnGUI() {

		//GUI.skin = gSkin;
	
		if ( smartFox.IsConnected ) {
			// Login

			GUI.Label(new Rect(10, 116, 100, 100), "Userame: ");
			username = GUI.TextField(new Rect(100, 116, 200, 20), username, 25);

			GUI.Label(new Rect(10, 218, 400, 100), loginErrorMessage);

			if ( GUI.Button(new Rect(100, 166, 100, 24), "Login")  || (Event.current.type == EventType.keyDown && Event.current.character == '\n')) {
				smartFox.Send(new LoginRequest(username, "", zone));
			}

		} else {
			GUI.Label(new Rect(10, 150, 400, 100), "Waiting for connection");
			GUI.Label(new Rect(10, 218, 400, 100), loginErrorMessage);
		}
	}

	/************
	 * Helper methods
	 ************/

	private void UnregisterSFSSceneCallbacks() {
		// This should be called when switching scenes, so callbacks from the backend do not trigger code in this scene
		smartFox.RemoveAllEventListeners();
	}

	/************
	 * Callbacks from the SFS API
	 ************/

	public void OnConnection(BaseEvent evt) {
		bool success = (bool)evt.Params["success"];
		string error = (string)evt.Params["errorMessage"];
		
		Debug.Log("On Connection callback got: " + success + " (error : <" + error + ">)");

		if (success) {
			SmartFoxConnection.Connection = smartFox;
		} else {
			loginErrorMessage = error;
		}
	}

	public void OnConnectionLost(BaseEvent evt) {
		loginErrorMessage = "Connection lost / no connection to server";
		UnregisterSFSSceneCallbacks();
	}

	public void OnDebugMessage(BaseEvent evt) {
		//string message = (string)evt.Params["message"];
		//Debug.Log("[SFS DEBUG] " + message);
	}

	public void OnLogin(BaseEvent evt) {
		bool success = true;
		if (evt.Params.ContainsKey("success") && !(bool)evt.Params["success"]) {
			// Login failed - lets display the error message sent to us
			loginErrorMessage = (string)evt.Params["errorMessage"];
			Debug.Log("Login error: "+loginErrorMessage);
		} else {
			// On to the lobby
			UnregisterSFSSceneCallbacks();
			Application.LoadLevel("game");
		}
	}
}