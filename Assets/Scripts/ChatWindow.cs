using UnityEngine;
using System.Collections;
using Sfs2X;
using Sfs2X.Requests;

public class ChatWindow {
	private ArrayList messages = new ArrayList();
	private string newMessage = "";
	private Vector2 chatScrollPosition;
	private int posX, posY, width, height;
	private SmartFox smartFox;
	private GUIStyle chatStyle;
	private GUIStyle userEventStyle;
	private GUIStyle systemStyle;
	
	private Rect chatWindow;

	public ChatWindow() {
		posX = 0;
		posY = 0;
		this.width = 100;
		this.height = 100;
		chatWindow = new Rect(posX, posY, this.width, this.height);
		smartFox = SmartFoxConnection.Connection;
		SetStyle();
	}

	public ChatWindow(int x, int y, int width, int height) {
		posX = x;
		posY = y;
		this.width = width;
		this.height = height;
		chatWindow = new Rect(posX, posY, this.width, this.height);
		smartFox = SmartFoxConnection.Connection;
		SetStyle();
	}

	private void SetStyle() {
		chatStyle = new GUIStyle();
		chatStyle.normal.textColor = new Color(0, 0, 0);
		chatStyle.wordWrap = true;
		userEventStyle = new GUIStyle();
		userEventStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
		userEventStyle.wordWrap = true;
		systemStyle = new GUIStyle();
		systemStyle.normal.textColor = new Color(1, 0, 0);
		systemStyle.wordWrap = true;
	}

	public void AddSystemMessage(string message) {
		messages.Add(new ChatMessage(ChatMessage.ChatType.SYSTEM, message));
		chatScrollPosition.y = 100000;
	}

	public void AddChatMessage(string message) {
		messages.Add(new ChatMessage(ChatMessage.ChatType.CHAT, message));
		chatScrollPosition.y = 100000;
	}

	public void AddPlayerJoinMessage(string message) {
		messages.Add(new ChatMessage(ChatMessage.ChatType.JOIN, message));
		chatScrollPosition.y = 100000;
	}

	public void AddPlayerLeftMessage(string message) {
		messages.Add(new ChatMessage(ChatMessage.ChatType.LEAVE, message));
		chatScrollPosition.y = 100000;
	}

	public void Draw() {
		chatWindow = GUI.Window(1, chatWindow, ShowChatWindow, "");
	}
	
	private void ShowChatWindow(int id) {	
		chatScrollPosition = GUILayout.BeginScrollView(chatScrollPosition, GUILayout.Width(width-20));
		if (messages.Count>0) {
			foreach (ChatMessage message in messages ) {
					GUILayout.BeginHorizontal();
					switch (message.GetChatType()) {
						case ChatMessage.ChatType.SYSTEM:
							GUILayout.Label(message.GetMessage(), systemStyle);
							break;
						case ChatMessage.ChatType.CHAT:
							GUILayout.Label(message.GetMessage(), chatStyle);
							break;
						case ChatMessage.ChatType.JOIN:
						case ChatMessage.ChatType.LEAVE:
							GUILayout.Label(message.GetMessage(), userEventStyle);
							break;
						default:
							// Ignore and dont print anything
							break;
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(1);
			}
		}
				
				
	    GUILayout.EndScrollView();
	    GUILayout.BeginHorizontal();
	   	newMessage = GUILayout.TextField(newMessage, GUILayout.Width(width-80));
	   	if (GUILayout.Button("Send", GUILayout.Width(60)) || (Event.current.type == EventType.keyDown && Event.current.character == '\n')) {
			smartFox.Send(new PublicMessageRequest(newMessage, null, smartFox.LastJoinedRoom));
			newMessage = "";
		}
	   	GUILayout.EndHorizontal();
	  	
	}

	internal class ChatMessage {
		public enum ChatType {
			IGNORE = 0,
			SYSTEM,
			CHAT,
			JOIN,
			LEAVE,
		};
		private ChatType type;
		private string message;

		public ChatMessage() {
			type = ChatType.IGNORE;
			message = "";
		}

		public ChatMessage(ChatType type, string message) {
			this.type = type;
			this.message = message;
		}

		public ChatType GetChatType() {
			return type;
		}

		public string GetMessage() {
			return message;
		}
	}
}
