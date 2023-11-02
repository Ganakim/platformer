using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  [Header("Hitboxes")]
  public Vector2 hitboxSize;
  public float hitboxOffset;
  private RaycastHit2D feet;
  private RaycastHit2D left;
  private RaycastHit2D right;

  [Header("Physics")]
  public bool canMove;
  public float moveSpeed;
  [Range(0, 1)]
  public float acceleration;
  public Vector2 squishIntensity;
  [Range(0, 1)]
  private Vector2 movement;

  public int airJumps;
  private int jumps;
  public float jumpForce;
  public Vector2 wallJumpForce;

  public float wallClingSmearDelay;
  [SerializeField] private float clingTime;
  [SerializeField] private int clingDir;
  [SerializeField] private int lastClingDir;
  [SerializeField] private GameObject clingingTo;

  public int bulletDahses;
  public float bulletDashForce;
  private int dashes;
  private Vector2 dashDir;

  private Rigidbody2D rb;
  private float gravScale;
  private float jumpCooldown = 0;

  private enum GroundType
  {
    None,
    Half,
    Full
  }
  [SerializeField] private GroundType groundType = GroundType.None;
  [SerializeField] private GameObject standingOn;

  void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    gravScale = rb.gravityScale;
    jumps = airJumps + 1;
    dashes = bulletDahses;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y - hitboxOffset, 0), transform.lossyScale * new Vector2(hitboxSize.y, hitboxSize.x));
    Gizmos.DrawWireCube(new Vector3(transform.position.x + hitboxOffset, transform.position.y, 0), transform.lossyScale * hitboxSize);
    Gizmos.DrawWireCube(new Vector3(transform.position.x - hitboxOffset, transform.position.y, 0), transform.lossyScale * hitboxSize);
  }

  void Update()
  {
    movement = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
    jumpCooldown = Mathf.Max(jumpCooldown - Time.deltaTime, 0);

    feet = Physics2D.BoxCast(transform.position, transform.lossyScale * new Vector2(hitboxSize.y, hitboxSize.x), 0f, Vector2.down, hitboxOffset, LayerMask.GetMask("Ground"));
    left = Physics2D.BoxCast(transform.position, transform.lossyScale * hitboxSize, 0f, Vector2.left, hitboxOffset, LayerMask.GetMask("Ground"));
    right = Physics2D.BoxCast(transform.position, transform.lossyScale * hitboxSize, 0f, Vector2.right, hitboxOffset, LayerMask.GetMask("Ground"));

    if (feet.collider?.gameObject != null)
    {
      standingOn = feet.collider?.gameObject;
      groundType = Enum.TryParse(standingOn.tag, true, out groundType) ? groundType : GroundType.None;
      if (jumpCooldown == 0) jumps = airJumps + 1;
    }
    else
    {
      groundType = GroundType.None;
      standingOn = null;
    }

    if (standingOn == null)
    {
      clingDir = 0;
      if (left.collider != null && left.collider.gameObject.tag == "Full" && movement.x < -.5f && lastClingDir != -1) clingDir--;
      else if (right.collider != null && right.collider.gameObject.tag == "Full" && movement.x > .5f && lastClingDir != 1) clingDir++;

      clingingTo = null;
      if (clingDir != 0) clingingTo = clingDir == -1 ? left.collider.gameObject : right.collider.gameObject;

      if (clingingTo != null)
      {
        if (clingTime == wallClingSmearDelay)
        {
          rb.velocity = new Vector2(0, 0);
          rb.gravityScale = 0;
          dashes = bulletDahses;
        }
        else if (clingTime < 0)
        {
          rb.gravityScale = gravScale * -clingTime;
        }
        if (Input.GetButtonDown("Jump") && clingTime > -1)
        {
          jumps++;
          lastClingDir = clingDir;
          clingTime = -1;
          Jump(clingDir);
        }
        clingTime = Mathf.Max(clingTime - Time.deltaTime, -1);
      }
      else
      {
        clingTime = wallClingSmearDelay;
        rb.gravityScale = gravScale;
      }
    }
    else
    {
      clingDir = 0;
      lastClingDir = 0;
      clingingTo = null;
      clingTime = wallClingSmearDelay;
      rb.gravityScale = gravScale;
      if (jumpCooldown == 0) dashes = bulletDahses;
    }

    if (standingOn?.transform != transform.parent) transform.SetParent(standingOn?.GetComponent<PlatformMove>() ? standingOn.transform : null, true);

    if (Input.GetButtonDown("Jump") && jumps > 0 && clingingTo == null) Jump();

    if (Input.GetAxisRaw("Vertical") < 0 && groundType == GroundType.Half) FallThrough(feet.collider);

    if (Input.GetAxisRaw("Vertical") > 0 && feet.collider != null && feet.collider.gameObject.tag == "Half") StartCoroutine(ResetCollision(feet.collider));

    if (Input.GetButtonDown("Fire2") && dashes > 0)
    {
      Vector2 v = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
      v.Normalize();
      BulletDash(v * bulletDashForce);
    }

    transform.localScale = new Vector2(1 - Mathf.Min(Mathf.Abs(rb.velocity.y), jumpForce) / jumpForce * (squishIntensity.y / 1f), 1 - Mathf.Abs(rb.velocity.x) / moveSpeed * (squishIntensity.x / 1f)) / (transform.parent?.lossyScale ?? Vector2.one);

    if (!clingingTo && canMove) rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x + movement.x * moveSpeed * acceleration, -moveSpeed, moveSpeed), rb.velocity.y);

    rb.velocity = new Vector2(rb.velocity.x * .8f, rb.velocity.y);

    if (dashDir.sqrMagnitude > 0)
    {
      rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * .8f);
      rb.velocity += dashDir;
      dashDir *= .98f;
      if (canMove = dashDir.sqrMagnitude < .1f) dashDir = Vector2.zero;
    }

    if (standingOn != null && (feet.collider.GetComponent<Rigidbody2D>() ?? false))
    {
      rb.velocity += feet.collider.GetComponent<Rigidbody2D>().velocity;
    }
  }

  void BulletDash(Vector2 dir)
  {
    dashes--;
    if (clingingTo != null) lastClingDir = clingDir;
    dashDir = dir;
    jumpCooldown = .1f;
  }

  void Jump(int dir = 0)
  {
    jumps--;
    rb.gravityScale = gravScale;
    if (clingingTo) BulletDash(new Vector2(wallJumpForce.x * -dir, wallJumpForce.y));
    else rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    jumpCooldown = .2f;
  }

  void FallThrough(Collider2D collider, float delay = .2f)
  {
    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collider, true);
    StartCoroutine(ResetCollision(collider, delay));
  }

  IEnumerator ResetCollision(Collider2D collider, float delay = 0f)
  {
    yield return new WaitForSeconds(delay);
    Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collider, false);
  }
}
