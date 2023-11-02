using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour{
  // public Weapon baseWeapon;
  public Transform reticle;
  public WeaponBaseData weaponBase;
  private SpriteRenderer spriteRenderer;
  [SerializeField] private float charge = 0;
  private float fireRate = 0;
  [SerializeField] private float holdTime = 0;

  void Awake(){
    if(reticle == null) reticle = transform.Find("Reticle");
    Cursor.lockState = CursorLockMode.Confined;
    Cursor.visible = false;
    spriteRenderer = GetComponent<SpriteRenderer>();
    spriteRenderer.sprite = weaponBase.model;
    charge = weaponBase.chargeTime;
    holdTime = weaponBase.holdTime;
  }

  void Update(){
    Vector3 cameraPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    reticle.position = new Vector3(cameraPos.x, cameraPos.y, 0);
    transform.right = reticle.position - transform.position;
    bool isAuto = weaponBase.fireMode == WeaponBaseData.FireMode.Auto;
    bool isSemi = weaponBase.fireMode == WeaponBaseData.FireMode.Semi;
    bool isBurst = weaponBase.fireMode == WeaponBaseData.FireMode.Burst;
    
    if(Input.GetButton("Fire1")){
      if((isAuto || Input.GetButtonDown("Fire1")) && (fireRate == 0 && charge == 0)) Fire(isBurst ? weaponBase.burstCount : 1);
      charge = Mathf.Max(charge - Time.deltaTime, 0);
      if(charge == 0) holdTime = Mathf.Max(holdTime - Time.deltaTime, 0);
    }else{
      charge = weaponBase.chargeTime;
    }
    if((Input.GetButtonUp("Fire1") && holdTime < weaponBase.holdTime) || (weaponBase.fireOnExpire && holdTime == 0)){
      Fire(isBurst ? weaponBase.burstCount : 1);
      holdTime = weaponBase.holdTime;
    }
    fireRate = Mathf.Max(fireRate - weaponBase.fireRate * 60 * Time.deltaTime, 0);
  }

  void Fire(int burstCount){
    Debug.Log("Pew!");
    burstCount--;
    for(int i = 0; i < weaponBase.projectilesPerShot; i++){
      GameObject projectile = weaponBase.projectile.Instantiate();
      projectile.AddComponent<ProjectileController>().projectileBase = weaponBase.projectile;
      projectile.transform.position = transform.position;
      projectile.transform.rotation = transform.rotation;
      projectile.GetComponent<Rigidbody2D>().velocity = transform.up * weaponBase.projectile.speed;
      projectile.transform.eulerAngles += new Vector3(0, 0, Random.Range(weaponBase.spreadMin, weaponBase.spreadMax));
      Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(), transform.parent.GetComponent<Collider2D>());
    }
    fireRate = weaponBase.fireRate;
    if(weaponBase.chargePerShot) charge = weaponBase.chargeTime;
    if(burstCount > 0){
      Invoke("Fire", weaponBase.burstDelay);
    }
  }
}

public class ProjectileController : MonoBehaviour{
  public ProjectileData projectileBase;
  private Rigidbody2D rb;

  void Awake(){
    rb = GetComponent<Rigidbody2D>();
  }

  void Start(){
    rb.velocity = transform.right * projectileBase.speed;
    Destroy(gameObject, projectileBase.lifespan);
  }
}
