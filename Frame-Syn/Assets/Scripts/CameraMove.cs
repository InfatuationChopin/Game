using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using SimpleJson;

public class CameraMove : MonoBehaviour
{
//	public static float MinX = -130;
//	public static float MinZ = -10;
//	public static float MaxX = 130;
//	public static float MaxZ = 10;
//	private float cursorWidth = 30;
//	private float screenEdge = 50;
//	private float speed = 30;
//	private float x;
//	private float y;
	private bool moveToPlayer = false;
	private bool lockToPlayer = true;

	private Vector2 lastTouch;

	void Start ()
	{
		lastTouch = new Vector2(0,0);
	}

	void Update ()
	{
		CameraFollow ();
	}

	void OnGUI ()
	{
		
		if (GUI.Button (new Rect (10, 30, 60, 20), "MyGod")) {
			lockToPlayer = !lockToPlayer;
			if (!lockToPlayer) {
				Camera.main.transform.position = new Vector3 (transform.position.x, 30, transform.position.z);
				Camera.main.transform.rotation = Quaternion.Euler (new Vector3 (90, 0, 0));
			}
		}
		if (GUI.Button (new Rect (10, 55, 60, 40), "Exit")) {
			JsonObject msg = new JsonObject ();
			PomeloCli.Notify ("center.matchHandler._clearDataAll", msg);
		}

		if (lockToPlayer) {
			return;
		}
		if (Event.current.mousePosition.x < 600 || Event.current.mousePosition.y > 200) {
			return;
		}

		if (Event.current.type == EventType.MouseDown) { //滑动开始  
			lastTouch = Event.current.mousePosition;
		}  
		if (Event.current.type == EventType.MouseDrag) { //滑动过程
			Vector2 changeTouch = Event.current.mousePosition - lastTouch;
			lastTouch = Event.current.mousePosition;

			Camera.main.transform.position += new Vector3 (changeTouch.x, 0, -changeTouch.y) * 0.1f;
		}  
		if (Event.current.type == EventType.MouseUp) { //滑动结束  
		}  
	}


	private void CameraFollow ()
	{
		if (Input.GetKeyDown (KeyCode.Space)) {
			moveToPlayer = true;
			return;
		}
		if (Input.GetKeyUp (KeyCode.Space)) {
			moveToPlayer = false;
			return;
		}
		if (Input.GetKeyUp (KeyCode.Y)) {
			lockToPlayer = !lockToPlayer;
			return;
		}
		if (moveToPlayer || lockToPlayer) {
			Camera.main.transform.position = new Vector3 (transform.position.x, transform.position.y + 10, transform.position.z - 8);
//			Camera.main.transform.rotation = Quaternion.Euler (new Vector3 (45, 45, 0));
			Camera.main.transform.rotation = Quaternion.Euler (new Vector3 (45, 0, 0));
			return;
		}

	}
}
