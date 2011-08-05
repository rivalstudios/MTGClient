using System;
using UnityEngine;

public class Player {
	
	private int id;
	private Transform transform;
	private Transform newTransform;
	
	public Player (int id, Transform transform) {
		this.id = id;
		this.transform = transform;
	}
	
	public float getX() {
		return transform.position.x;
	}
	
	public float getY() {
		return transform.position.y;
	}
	
	public float get() {
		return transform.position.z;
	}
	
	public void setPosition(Vector3 pos) {
		transform.position = pos;
	}
	
	public Transform getTransform() {
		return transform;
	}
	
	public int getId() {
		return id;
	}
	
	public void setId(int id) {
		this.id = id;
	}
}

