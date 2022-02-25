using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;

    public bool invertLook;
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private Vector3 movement;
    public CharacterController characterController;
    private Camera camera;
    public float jumpForce = 9f;
    public float gravityMod = 2.5f;
    public Transform groundCheckPoint;
    public bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    //public float fireRate = 0.1f;
    private float shotCounter;
    private float maxHeat = 10f;
    //private float heatPerShot = 1f;
    private float coolRate = 4f;
    private float overheatCoolRate = 2f;
    private float heatCounter;
    private bool isOverheated;

    public List<Gun> gunList;
    private int selectedGun;

    public float muzzleDisplayTime;
    private float muzzleCounter;

    public GameObject playerHitImpact;
    


    //public bool isAutomatic;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        camera = Camera.main;
        
        UiController.instance.weaponTempSlider.maxValue = maxHeat;
        selectedGun = 2;
        switchGun();
        // Offline (no photon) spawnpoint testing
        /*Transform spawnPoint = SpawnManager.instance.pickSpawnPoint();
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;*/
    }

    // Update is called once per frame
    void Update()
    {
        
        if(photonView.IsMine) {
            // Cursor movement on screen * mouse sensitivity
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
            // Left Right on the body
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);
            // Ensure game does not flip over/snap back due to there being no negative angles for eulerAngles.x (0-360)
            // e.g. 350 = -10 degrees
            verticalRotStore = verticalRotStore + mouseInput.y;
            verticalRotStore = Mathf.Clamp(verticalRotStore, -80f, 80f);
            
            // Up Down on the camera view on the head (clamped)
            if(invertLook) {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
            } else {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
            }
            
            // X-axis is for horizontal movement, y is for vertical, z is for forwards/backwards
            moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            
            // LShift to run
            if (Input.GetKey(KeyCode.LeftShift)) 
            {
                activeMoveSpeed = runSpeed;
            } else {
                activeMoveSpeed = moveSpeed;
            }
            // Record the current y movement for smooth falling (1)
            float yVelocity = movement.y;
            // movement is a Vector3()
            // transform.forward refers to the object's forward direction, which is the local z axis
            // transform.right same thing local x axis
            // normalized so that diagonal movement is not faster, then * the walk/run speed
            movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
            // set y to the current y movement for smooth falling (2)
            // otherwise movement.y is set to default 0 at every frame, which would cause the player to fall very slowly since gravity is not accelerating the movement
            movement.y = yVelocity;
            // Set y movement to 0 if on the ground
            if (characterController.isGrounded) {
                movement.y = 0f;
            }
            // Shoot very small raycast straight down and if contact with ground layer, true else false
            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
            // Space bar for jumping
            if(Input.GetButtonDown("Jump") && isGrounded) {
                movement.y += jumpForce;
            }
            // Subtract gravity * gravityMod from the y movement 
            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
            //transform.position = transform.position + moveDirection * moveSpeed * Time.deltaTime;
            // Use CharacterController instead
            //transform.position = transform.position + movement * moveSpeed * Time.deltaTime;
        
            characterController.Move(movement * Time.deltaTime);
            // Free cursor on Esc
            if(Input.GetKeyDown(KeyCode.Escape)) {
                Cursor.lockState = CursorLockMode.None;
        // Lock cursor on window click
            } else if (Cursor.lockState == CursorLockMode.None)
            {
            if (Input.GetMouseButtonDown(0)) {
                Cursor.lockState = CursorLockMode.Locked;
            }
            }

            // To limit the number of muzzle flashes for higher frames per second
            if (gunList[selectedGun].muzzleFlash.activeInHierarchy) {
                muzzleCounter -= Time.deltaTime;
                if (muzzleCounter <= 0) {
                    gunList[selectedGun].muzzleFlash.gameObject.SetActive(false);
                }
            }
            shotCounter -= Time.deltaTime;

            if(Input.GetMouseButtonDown(0) && !isOverheated) {
                if (shotCounter <= 0) {
                    Shoot();
                }
            }

            
            if(Input.GetMouseButton(0) && gunList[selectedGun].isAutomatic && !isOverheated) {
                if (shotCounter <= 0) {
                    Shoot();
                }
                
            }

            // Weapon heat management
            if (isOverheated) {
                heatCounter -= overheatCoolRate * Time.deltaTime;
                if (heatCounter <= 0) {
                    heatCounter = 0;
                    isOverheated = false;
                    UiController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            } else {
                if (heatCounter > 0) {
                    heatCounter -= coolRate * Time.deltaTime;
                    if (heatCounter <= 0) {
                        heatCounter = 0;
                    }
                }
            }
            UiController.instance.weaponTempSlider.value = heatCounter;

            //Gun selection via number buttons
            // if (Input.GetKeyDown(KeyCode.Alpha1)) {
            //     selectedGun = 0;
            //     switchGun();
            // } 

            // else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            //     selectedGun = 1;
            //     switchGun();
            // }

            // else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            //     selectedGun = 2;
            //     switchGun();
            // }
            // // Gun selection via scroll wheel
            // // Scroll wheel upwards
            // if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f) {
            //     selectedGun++;
            //     if (selectedGun >= gunList.Count) {
            //         selectedGun = 0;
            //     }
            //     switchGun();
            // }
            // // Scroll wheel downwards
            // else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f) {
            //     selectedGun--;
            //     if (selectedGun < 0) {
            //         selectedGun = gunList.Count - 1;
            //     }
            //     switchGun();
            // }
        }
    }

    

    private void Shoot() {
        // Shoot a ray from the camera with direction to the point in the middle of the screen
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ray.origin = camera.transform.position;
        // Raycast stores what the ray hits in 'hit' and sends it to 'out'
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            //Debug.Log("Hit " + hit.collider);
            if (hit.collider.gameObject.tag == "Player") {
                // Player hit sparkle effect
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                // Call RPC function of the 'hit' to tell everyone that this player dealt damage to 'hit', parameters come after the target
                 hit.collider.gameObject.GetPhotonView().RPC("dealDamage", RpcTarget.All, photonView.Owner.NickName, PhotonNetwork.LocalPlayer.ActorNumber);

            } else {
            
                // Create bulletImpact object at the hit.point, rotate it so that its normal matches the surface it hit
                // (hit.normal * 0.002f) so that it is slightly above the surface
                GameObject bulletImpactTemp = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                // remove the bulletImpact objects after 6 seconds
                Destroy(bulletImpactTemp, 6f);
            }
        }

        shotCounter = gunList[selectedGun].fireRate;

        heatCounter += gunList[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat) {
            heatCounter = maxHeat;
            isOverheated = true;
            UiController.instance.overheatedMessage.gameObject.SetActive(true);
        }
        gunList[selectedGun].muzzleFlash.gameObject.SetActive(true);
        // To limit the number of muzzle flashes for higher frames per second
        muzzleCounter = muzzleDisplayTime;

    }
    [PunRPC]
    public void dealDamage(string damageDealer, int killerActor) {
        takeDamage(damageDealer, killerActor);
    }

    public void takeDamage(string damageDealer, int killerActor) {
        // if I get shot, I die :(
        if (photonView.IsMine) {
            if (!Input.GetKey("w") && !Input.GetKey("a") && !Input.GetKey("s") && !Input.GetKey("d") && isGrounded) {
                Debug.Log("Missed.");
            }
            else {
                Debug.Log("Hit.");
                PlayerSpawner.instance.playerDeath(damageDealer);
                MatchManager.instance.updatePlayerSend(killerActor, 0, 1);
            }
        }
    }

    
    void switchGun() {
        foreach(Gun gun in gunList) {
            gun.gameObject.SetActive(false);
        }
        gunList[selectedGun].gameObject.SetActive(true);
        // Edge case for switching gun on same frame of muzzleFlash
        gunList[selectedGun].muzzleFlash.SetActive(false);
        
    }

    private void OnTriggerEnter(Collider other) {
        if (gameObject.tag == "Player" && other.tag == "FinishLine") {
            MatchManager.instance.state = MatchManager.GameState.Ending;
            MatchManager.instance.isGmWin = false;
            MatchManager.instance.winnerName = photonView.Owner.NickName;
            MatchManager.instance.StateCheck();
        }
    }
    private void LateUpdate() {
        // Only move camera for the owner of the player
        if (photonView.IsMine) {
            // Move camera to the player's head position, and move/rotate with it
            camera.transform.position = viewPoint.position;
            camera.transform.rotation = viewPoint.rotation;
        }
    }
}
