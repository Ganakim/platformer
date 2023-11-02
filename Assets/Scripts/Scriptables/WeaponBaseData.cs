using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBaseData", menuName = "Scriptables/WeaponBaseData", order = 0)]
public class WeaponBaseData : ScriptableObject {
    public Sprite model;
    public ProjectileData projectile;
    public enum FireMode {Semi, Auto, Burst};
    public FireMode fireMode;
    public float fireRate = 1;
    public float chargeTime = 0;
    public float holdTime = 0;
    public bool chargePerShot = false;
    public bool fireOnExpire = false;
    public int projectilesPerShot = 1;
    public int burstCount = 1;
    public float burstDelay = 0;
    [Range(-360, 360)]
    public float spreadMin = 0;
    [Range(-360, 360)]
    public float spreadMax = 0;
    public int ammoCapacity;
    public float reloadTime;
}
