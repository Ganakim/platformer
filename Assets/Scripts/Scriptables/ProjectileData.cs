using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptables/ProjectileData", order = 1)]
public class ProjectileData : ScriptableObject {
    public Sprite model;
    public bool homing = false;
    public bool sticky = false;
    public bool explodesOnExpire = false;
    public float explosiveRadius = 0;
    public float explosiveDamage = 0;
    public Vector2 gravity = new Vector2(0, 0);
    public int bounces = 0;
    public int pierces = 0;
    public int chains = 0;
    public ProjectileData splitProjectile;
    public float splitTime = 0;
    public int splitCount = 0;
    [Range(-360, 360)]
    public float splitAngleMin = 0;
    [Range(-360, 360)]
    public float splitAngleMax = 0;
    public float speed = 1;
    public float damage = 1;
    public float knockback = 0;
    [Range(-360, 360)]
    public float knockbackAngleMin = 0;
    [Range(-360, 360)]
    public float knockbackAngleMax = 0;
    public float lifespan = 0;

    public GameObject Instantiate(){
        GameObject projectile = new GameObject();
        projectile.AddComponent<SpriteRenderer>();
        projectile.GetComponent<SpriteRenderer>().sprite = model;
        projectile.AddComponent<Rigidbody2D>();
        projectile.AddComponent<BoxCollider2D>();
        projectile.GetComponent<Rigidbody2D>().gravityScale = gravity.y;
        projectile.GetComponent<Rigidbody2D>().mass = 0.1f;
        projectile.GetComponent<Rigidbody2D>().freezeRotation = true;
        return projectile;
    }
}
