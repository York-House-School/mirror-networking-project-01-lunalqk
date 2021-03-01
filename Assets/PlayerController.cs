﻿using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : NetworkBehaviour {
	
	[Header("Spawn")]
	[SerializeField] GameObject[] spawnLocations;
	
	[Header("Player stats")]
    [SyncVar] public int Health = 100;
    [SyncVar] public int HealthMax = 100;
    [SyncVar] public int Kills;
    [SyncVar] public int Deaths;
    [SyncVar] public bool isDead;

	[Header("Player speed")]
	[SerializeField] float PlayerSpeed;
	[SerializeField] float PlayerSpeedMax;
	[SerializeField] Vector3 jump;
    [SerializeField] float jumpForce = 2.0f;
	[SerializeField] bool isGrounded;

	[Header("Sensitivity")]
	[SerializeField] float sensitivityX = 1f;
	[SerializeField] float sensitivityY = 1f;
	[SerializeField] float maxCameraX = 80f;
	[SerializeField] float minCameraX = -80f;

	[Header("Weapon")]
	[SyncVar] int AmmoCountMax = 30;
	[SyncVar] public int AmmoCount = 30;
	[SyncVar] bool Reloading;
	[SerializeField] double reloadTime = 2;
	[SerializeField] int weaponDamage = 1;
	[SerializeField] float WeaponCooldown;
	
	[Header("GFX")]
    [SerializeField] GameObject[] disableOnClient;
    [SerializeField] GameObject[] disableOnDeath;

	[Header("Camera")]
	[SerializeField] Vector3 cameraOffset;

	[Header("Debug")]
	[SerializeField] bool DebugMode = false;


	Rigidbody rb;
	float xRotation;
	float curCooldown;
	Animator anim;
	bool inRespawn = false;


	GameObject cameraRig;

	void Start () {
		anim = GetComponent<Animator>();
		rb = GetComponent<Rigidbody>();
		
		//jump = new Vector3(0.0f, 2.0f, 0.0f);
		
		spawnLocations = GameObject.FindGameObjectsWithTag("SpawnPoint");
		cameraRig = GameObject.FindWithTag("OVRRig");


		SpawnPlayer();
		
		if (isLocalPlayer) {

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			//Setup FPS camera.
			cameraRig.transform.SetParent(transform);
			cameraRig.transform.localPosition = cameraOffset;
			cameraRig.transform.rotation = Quaternion.identity;
			/*Camera.main.transform.SetParent(transform);
			Camera.main.transform.localPosition = cameraOffset;
			Camera.main.transform.rotation = Quaternion.identity;*/

			CanvasManager.instance.ChangePlayerState(true);
			CanvasManager.instance.UpdateHP(Health , HealthMax);
			CanvasManager.instance.localPlayer = this;
			CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
			
			foreach (GameObject go in disableOnClient) {
                go.SetActive(false);
            }
		
		} else {
			rb.isKinematic = true;
		}
	}
		
	void Update () {
		if (!isLocalPlayer) return;

		if (DebugMode) {
			Camera.main.transform.localPosition = cameraOffset;
		}
		
		curCooldown -= Time.deltaTime;


		if (!isDead) {

			Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            if (stick.x != 0.0f || stick.y != 0.0f  )
            {
                anim.SetBool("isRunning", true);
                MoveInput();
            }
            else
            {
                anim.SetBool("isRunning", false);
            }

            if (OVRInput.GetDown(OVRInput.Button.One) && isGrounded) {
				rb.AddForce(jump * jumpForce, ForceMode.Impulse);
				isGrounded = false;
			}

			//MouseLook();

			//Debug.Log(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger));

			if (OVRInput.GetDown(OVRInput.Touch.Two))
			{
                ShootButton();
                Debug.Log("update" + AmmoCount);
            }

            if (OVRInput.GetDown(OVRInput.Touch.Four)) {
				ReloadButton();
			}
        }
        else
        {
			CanvasManager.instance.ChangePlayerState(false);
		}
		
		if (Kills >= 5) {
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
			
			foreach (GameObject p in players) {
				p.GetComponent<PlayerController>().isDead = true;	
				p.GetComponent<PlayerController>().CmdEndGame();
			}
		}

		CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
		CanvasManager.instance.UpdateHP(Health, HealthMax);
	}

	
	
	
	void OnCollisionStay() {
		isGrounded = true;
	}
	
	void SpawnPlayer() {
		int loc = Random.Range(0, spawnLocations.Length);
		this.transform.position = spawnLocations[loc].transform.position;
		this.transform.rotation = spawnLocations[loc].transform.rotation;
	}


	private void MoveInput() {

		Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

		//Input
		if (stick.x != 0.0f || stick.y != 0.0f)	{
			Vector3 movementDirection = new Vector3(stick.x, 0, stick.y);
			movementDirection *= PlayerSpeed;
			movementDirection = Vector3.ClampMagnitude(movementDirection, PlayerSpeed);

			if (rb.velocity.magnitude < PlayerSpeedMax)
				rb.AddRelativeForce(movementDirection * Time.deltaTime * 100);
		}
	}

	private void MouseLook() {
		float rotY = Input.GetAxis ("Mouse X") * sensitivityY;
		float rotX = -Input.GetAxis ("Mouse Y") * sensitivityX;

		//Body rotation
		transform.Rotate(0, rotY, 0);

		//Camera rotation
		xRotation = Mathf.Clamp(xRotation + rotX, minCameraX, maxCameraX);
		//Debug.Log("" + xRotation);
		//weaponArm.localEulerAngles = new Vector3(xRotation, 0, 0);
		Camera.main.transform.localEulerAngles = new Vector3(xRotation, 0, 0);
	}

	/*private void VRInput()
	{

		Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

		//Input
		if (stick.x != Mathf.Epsilon || stick.y != Mathf.Epsilon)
		{
			Vector3 movementDirection = new Vector3(stick.x, 0, CanvasManager.instance.leftJoystick.Vertical);
			movementDirection *= PlayerSpeed;
			movementDirection = Vector3.ClampMagnitude(movementDirection, PlayerSpeed);

			if (rb.velocity.magnitude < PlayerSpeedMax)
				rb.AddRelativeForce(movementDirection * Time.deltaTime * 100);
		}

		//Rotation
		if (stick.x != Mathf.Epsilon || CanvasManager.instance.rightJoystick.Vertical != Mathf.Epsilon)
		{
			float rotY = stick.x * sensitivityY;
			float rotX = -CanvasManager.instance.rightJoystick.Vertical * sensitivityX;

			//Body rotation
			transform.Rotate(0, rotY, 0);

			//Camera rotation
			xRotation = Mathf.Clamp(xRotation + rotX, minCameraX, maxCameraX);
			weaponArm.localEulerAngles = new Vector3(xRotation, 0, 0);
			Camera.main.transform.localEulerAngles = new Vector3(xRotation, 0, 0);
		}

		if (stick.x != Mathf.Epsilon || CanvasManager.instance.shootJoystick.Vertical != Mathf.Epsilon)
		{
			float rotY = stick.x * sensitivityY;
			float rotX = -CanvasManager.instance.shootJoystick.Vertical * sensitivityX;

			//Body rotation
			transform.Rotate(0, rotY, 0);

			//Camera rotation
			xRotation = Mathf.Clamp(xRotation + rotX, minCameraX, maxCameraX);
			weaponArm.localEulerAngles = new Vector3(xRotation, 0, 0);
			Camera.main.transform.localEulerAngles = new Vector3(xRotation, 0, 0);
		}
	}*/







	//
	// SHOOTING
	//
	internal void ShootButton() {
        //First local if can shoot check.
        //if ammoCount > 0 && isAlive
        if (Reloading == false && AmmoCount > 0 && !isDead && curCooldown < 0.01f) {
            //Do command
            CmdTryShoot(Camera.main.transform.forward, Camera.main.transform.position);
            curCooldown = WeaponCooldown;
			Debug.Log("after" + AmmoCount);
		}
    }
	
	[Command]
	void CmdTryShoot(Vector3 clientCam, Vector3 clientCamPos) {
        //Server side check
        //if ammoCount > 0 && isAlive
        if (AmmoCount > 0 && !isDead) {
            AmmoCount--;
            TargetShoot();

			Ray ray = new Ray(clientCamPos, clientCam * 500);
            //Debug.DrawRay(clientCamPos, clientCam * 500, Color.red, 2f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {

                Debug.Log("SERVER: Player shot: " + hit.collider.name);
                if (hit.collider.CompareTag("Player") && hit.collider.GetComponent<PlayerController>().isDead==false) {
                    //RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                    hit.collider.GetComponent<PlayerController>().Damage(weaponDamage, GetComponent<NetworkIdentity>().netId);
                }
                else {
                    //RpcPlayerFired(GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                }

            }
        }

    }
	
	[TargetRpc]
    void TargetShoot() {
		//We shot successfully.
		//Update UI
		Debug.Log("changed" + AmmoCount);
		CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
    }
	
	[ClientRpc]
    void RpcPlayerFired(uint shooterID, Vector3 impactPos, Vector3 impactRot) {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        //Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        //NetworkIdentity.spawned[shooterID].GetComponent<PlayerController>().MuzzleFlash();
    }
	
	[ClientRpc]
    void RpcPlayerFiredEntity(uint shooterID, uint targetID, Vector3 impactPos, Vector3 impactRot) {
        //Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkIdentity.spawned[targetID].transform);
        //Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        //NetworkIdentity.spawned[shooterID].GetComponent<PlayerController>().MuzzleFlash();

    }
	
	
	
	
	
	
	
	[Server]
    public void Damage(int amount, uint shooterID) {
        Health -= amount;
        TargetGotDamage();
        if (Health < 1) {
            Die();
            NetworkIdentity.spawned[shooterID].GetComponent<PlayerController>().Kills++;
            NetworkIdentity.spawned[shooterID].GetComponent<PlayerController>().TargetGotKill();
			TargetGotDamage();
		}
    }
	
	[TargetRpc]
    public void TargetGotKill() {
        Debug.Log("You got kill.");
    }
	
	[TargetRpc]
    public void TargetGotDamage() {
        CanvasManager.instance.UpdateHP(Health, HealthMax);
        Debug.Log("We got hit!");
    }
	
	[Server]
    public void Die() {
        Deaths++;
        isDead = true;
        Debug.Log("SERVER: Player died.");
        TargetDie();
        RpcPlayerDie();
    }
	
	[TargetRpc]
    void TargetDie() {
        //Called on the died player.
        CanvasManager.instance.ChangePlayerState(!isDead);
        Debug.Log("You died.");
    }
	
	[ClientRpc]
    void RpcPlayerDie() {
        GetComponent<Collider>().enabled = false;
        foreach (GameObject item in disableOnDeath)
        {
            item.SetActive(false);
        }
    }
	
	
	
	
	
	
	
	
	
	
	
	
	//
	//RELOADING
	//
    internal void ReloadButton() {
        if(Reloading || AmmoCount != AmmoCountMax)
            CmdTryReload();
    }
	
	[Command]
    void CmdTryReload() {
        if (Reloading || AmmoCount == AmmoCountMax)
            return;

        StartCoroutine(reloadingWeapon());
    }
	
    IEnumerator reloadingWeapon() {
        Reloading = true;
        yield return new WaitForSeconds((float)reloadTime);
        AmmoCount = AmmoCountMax;
        TargetReload();
        Reloading = false;

        yield return null;
    }
	
	[TargetRpc]
    void TargetReload()
    {
        //We reloaded successfully.
        //Update UI
        CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
    }
	
	
	
	
	
	
	
	
	
	
	
	
	
	
	//
	// RESPAWN
	//
	[Command]
    public void CmdRespawn() {
        //Check if dead
        if (isDead) {
            Health = HealthMax;
            AmmoCount = AmmoCountMax;
            isDead = false;
            TargetRespawn();
            RpcPlayerRespawn();
        }
    }
	
	[TargetRpc]
    void TargetRespawn() {
        CanvasManager.instance.ChangePlayerState(true);
		CanvasManager.instance.UpdateHP(Health, HealthMax);
        //set position
        //transform.position = NetworkManagerFPS.singleton.GetStartPosition().position
		SpawnPlayer();

    }
	
	[ClientRpc]
    void RpcPlayerRespawn() {
        GetComponent<Collider>().enabled = true;

        foreach (GameObject item in disableOnDeath) {
            item.SetActive(true);
        }
    }
	
	
	
	
	
	
	
	
	
	
	[Command]
	public void CmdEndGame(){
		CanvasManager.instance.GameOverUI();
	}
	
}
