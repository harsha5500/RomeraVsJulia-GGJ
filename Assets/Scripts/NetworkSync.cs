using UnityEngine;
using System.Collections;

public class NetworkSync : MonoBehaviour
{
	public GameObject gameManager;
	public float speed = 10f;
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	
	// --- ENEMY VARIABLES ---
	public Vector3 enemyPos;

	// --- PLAYER VARIABLES ---
	public string playerName;
	public Vector3 playerPos;
	public bool playerInRange;

	public enum playerStates
	{
Search,
		Avoid,
		Follow,
		Caught}

	;

	public bool playerDeath;
	public playerStates state = playerStates.Search;

	public enum direction
	{
up,
		down,
		left,
		right}

	;

	public direction playerDirection = direction.up;
	public direction prevDirection = direction.up;

	//CAMERA
	Transform Object1;

	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		if (stream.isWriting) {
			syncPosition = GetComponent<Rigidbody> ().position;
			stream.Serialize (ref syncPosition);

			syncPosition = GetComponent<Rigidbody> ().velocity;
			stream.Serialize (ref syncVelocity);
		} else {
			stream.Serialize (ref syncPosition);
			stream.Serialize (ref syncVelocity);

			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;

			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = GetComponent<Rigidbody> ().position;
		}
	}

	void Start ()
	{

		gameManager = GameObject.FindWithTag ("GameController");

		if (GetComponent<NetworkView>().isMine) {
			Object1 = GameObject.FindWithTag ("MainCamera").transform;
			Object1.parent = transform;
			Object1.transform.position = transform.localPosition + new Vector3 (0f, 15f, -5f);

			playerInRange = false;
			state = playerStates.Search;
		}
	}

	void Awake ()
	{
		lastSynchronizationTime = Time.time;
	}

	void Update ()
	{
		if (GetComponent<NetworkView> ().isMine) {
			InputMovement ();
			/*GetComponent<MouseLook>().enabled = true;
			GetComponent<CharacterMotor>().enabled = true;
			GetComponent<CharacterMotor>().enabled = true;*/

			//Keep tabs on the players locations and distance from each other.
			playerPos = this.transform.position;
			enemyPos = FindClosestEnemy ().transform.position;
			var distance = Vector3.Distance (this.transform.position, FindClosestEnemy ().transform.position);
			Vector3 rota;
			if (prevDirection != playerDirection) {
				switch (playerDirection) {
				case direction.up:
					rota = new Vector3 (0, 0, 0);
					transform.GetChild (1).eulerAngles = rota;
					break;
				case direction.down:
					rota = new Vector3 (100, 180, 100);
					transform.GetChild (1).eulerAngles = rota;
					break;
				case direction.left:
					rota = new Vector3 (100, 270, 100);
					transform.GetChild (1).eulerAngles = rota;
					break;
				case direction.right:
					rota = new Vector3 (100, 90, 100);
					transform.GetChild (1).eulerAngles = rota;
					break;
				}
				prevDirection = playerDirection;
			}
			//Determine if player is close to the enemy. If so change state. We'll alert them later.
			if (distance < 10.0f) {
				playerInRange = true;
				//TODO: GUI - Show on screen the players state.
				//print(state);
			} else {
				playerInRange = false;
			}

		} else {
			gameObject.tag = "Enemy";
			SyncedMovement ();
		}
	}

	//Handy little function to help us find the closest enemy. With that done we can pull information regarding them.
	GameObject FindClosestEnemy ()
	{
		GameObject[] gos;
		gos = GameObject.FindGameObjectsWithTag ("Enemy");
		GameObject closest = null;
		float distance = Mathf.Infinity;
		Vector3 position = transform.position;
		foreach (GameObject go in gos) {
			Vector3 diff = go.transform.position - position;
			float curDistance = diff.sqrMagnitude;
			if (curDistance < distance) {
				closest = go;
				distance = curDistance;
			}
		}
		return closest;
	}
	
	//Tag: Determine results when a player tags another
	void OnTriggerEnter (Collider other)
	{
		if (other.gameObject.CompareTag ("Enemy")) {
			GameManager manager = gameManager.GetComponent<GameManager> ();

			//	if(state == playerStates.Follow) {
			if (playerName == "Player A") {
				manager.playerAScore++;
			}
			/*		if(playerName == "Player B") {
					manager.playerBScore ++;
				}
			}
			if(state == playerStates.Avoid) {
				state = playerStates.Caught;
				if(playerName == "Player A") {
					manager.playerAScore --;
				} */
			if (playerName == "Player B") {
				manager.playerBScore--;
			}
			//}
		}
	}

	private void InputMovement ()
	{
		if (Input.GetKey (KeyCode.W)) {
			GetComponent<Rigidbody> ().MovePosition (GetComponent<Rigidbody> ().position + Vector3.forward * speed * Time.deltaTime);
			playerDirection = direction.up;
		}
		if (Input.GetKey (KeyCode.S)) {
			GetComponent<Rigidbody> ().MovePosition (GetComponent<Rigidbody> ().position - Vector3.forward * speed * Time.deltaTime);
			playerDirection = direction.down;
		}
		if (Input.GetKey (KeyCode.D)) {
			GetComponent<Rigidbody> ().MovePosition (GetComponent<Rigidbody> ().position + Vector3.right * speed * Time.deltaTime);
			playerDirection = direction.right;
		}
		if (Input.GetKey (KeyCode.A)) {
			GetComponent<Rigidbody> ().MovePosition (GetComponent<Rigidbody> ().position - Vector3.right * speed * Time.deltaTime);
			playerDirection = direction.left;
		}
	}

	private void SyncedMovement ()
	{
		syncTime += Time.deltaTime;
		GetComponent<Rigidbody> ().position = Vector3.Lerp (syncStartPosition, syncEndPosition, syncTime / syncDelay);
	}

}
