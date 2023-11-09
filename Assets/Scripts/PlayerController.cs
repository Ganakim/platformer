using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  [Header("Hitboxes")]
  public Vector2 handHitboxSize;
  public Vector2 feetHitboxSize;
  private Collider2D left;
  private Collider2D right;
  private Collider2D standingOn;
  private enum GroundType
  {
    None,
    Half,
    Full
  }
  private GroundType groundType = GroundType.None;

  [Header("Physics")]
  public bool canMove;
  public float moveSpeed;
  [Range(0, 1)]
  public float acceleration;
  public Vector2 squishIntensity;
  public float crouchSpeed;
  private float crouchAmount;
  private Vector2 movement;

  [Header("Jumping")]
  public int airJumps;
  private int jumps;
  public float jumpForce;
  public Vector2 wallJumpForce;

  [Header("Wall Clinging")]
  public float wallClingSmearDelay;
  private int lastClingDir;
  private float clingTime;

  [Header("Bullet Dashing")]
  public int bulletDashes;
  public float bulletDashForce;
  private int dashes;

  private Rigidbody2D rb;
  private Collider2D c;
  private Vector2 originalScale;
  private float jumpCooldown = 0;

  void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    c = GetComponent<Collider2D>();
    originalScale = transform.localScale;
    jumps = airJumps + 1;
    dashes = bulletDashes;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y - (.5f + feetHitboxSize.y / 2f), 0), transform.lossyScale * feetHitboxSize);
    Gizmos.DrawWireCube(new Vector3(transform.position.x + (.5f + handHitboxSize.x / 2f), transform.position.y, 0), transform.lossyScale * handHitboxSize);
    Gizmos.DrawWireCube(new Vector3(transform.position.x - (.5f + handHitboxSize.x / 2f), transform.position.y, 0), transform.lossyScale * handHitboxSize);
  }

  void Update()
  {
    movement = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
    if (originalScale == Vector2.zero && rb.velocity == Vector2.zero)
    {
      Debug.Log("LazyLoading the scale");
      originalScale = transform.localScale;
    }
    jumpCooldown = Mathf.Max(jumpCooldown - Time.deltaTime, 0);

    CheckGrounded();
    CheckWalls();

    float crouch = Input.GetAxisRaw("Vertical");
    if (canMove) Move(crouch);

    transform.localScale = new Vector2(
      originalScale.x - Mathf.Min(Mathf.Abs(rb.velocity.y), jumpForce) * squishIntensity.x * originalScale.x / jumpForce,
      originalScale.y - Mathf.Abs(rb.velocity.x) * squishIntensity.x * originalScale.y / moveSpeed
    ) / (transform.parent?.lossyScale ?? Vector2.one);

    if (groundType == GroundType.Full)
    {
      Vector2 originalLossyScale = originalScale / (transform.parent?.lossyScale ?? Vector2.one);
      float targetSize = originalLossyScale.y - squishIntensity.y;
      crouchAmount = Mathf.MoveTowards(crouchAmount, -crouch, crouchSpeed * Time.deltaTime);
      transform.localScale = new Vector2(transform.localScale.x, Mathf.Lerp(transform.localScale.y, targetSize, crouchAmount));
    }
  }

  void Move(float crouch)
  {
    rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, movement.x * (moveSpeed - (moveSpeed * Math.Max(-crouch, 0) / 2)), acceleration), rb.velocity.y);

    if (standingOn != null && jumpCooldown == 0)
    {
      jumps = airJumps + 1;
      dashes = bulletDashes;
      clingTime = wallClingSmearDelay;
      lastClingDir = 0;
    }

    int dir = 0;
    if (left != null && movement.x < 0) dir--;
    if (right != null && movement.x > 0) dir++;
    if (standingOn == null && dir != lastClingDir && dir != 0) WallCling(dir);

    if (Input.GetButtonDown("Jump")) Jump();
    if (Input.GetButtonDown("Fire2")) BulletDash(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position);

    if (crouch < 0 && groundType == GroundType.Half) FallThrough(standingOn);
    if (crouch > 0 && standingOn != null && groundType == GroundType.Half) StartCoroutine(ResetCollision(standingOn));
  }

  void CheckGrounded()
  {
    standingOn = Physics2D.BoxCast(transform.position, transform.lossyScale * feetHitboxSize, 0f, Vector2.down, .5f + feetHitboxSize.y / 2f, LayerMask.GetMask("Ground")).collider;
    groundType = Enum.TryParse(standingOn?.tag, true, out groundType) ? groundType : GroundType.None;
    if (standingOn?.transform != transform.parent) transform.SetParent(standingOn?.GetComponent<PlatformMove>() ? standingOn.transform : null, true);
  }

  void CheckWalls()
  {
    RaycastHit2D[] leftHits = Physics2D.BoxCastAll(transform.position, transform.lossyScale * handHitboxSize, 0f, Vector2.left, .5f + handHitboxSize.x / 2f, LayerMask.GetMask("Ground"), 0, 0);
    RaycastHit2D[] rightHits = Physics2D.BoxCastAll(transform.position, transform.lossyScale * handHitboxSize, 0f, Vector2.right, .5f + handHitboxSize.x / 2f, LayerMask.GetMask("Ground"), 0, 0);
    left = null;
    right = null;
    foreach (RaycastHit2D hit in leftHits) if (hit.collider.CompareTag("Full")) left = hit.collider;
    foreach (RaycastHit2D hit in rightHits) if (hit.collider.CompareTag("Full")) right = hit.collider;
    if (left == null && right == null) clingTime = wallClingSmearDelay;
  }

  void WallCling(int clingDir)
  {
    dashes = bulletDashes;
    if (clingTime >= 0) rb.velocity = new Vector2(rb.velocity.x, 1);
    else rb.velocity = new Vector2(rb.velocity.x, Physics2D.gravity.y * -clingTime);

    clingTime = Mathf.Max(clingTime - Time.deltaTime, -1);
    if (clingTime == -1) lastClingDir = clingDir;
  }

  void BulletDash(Vector2 dir)
  {
    int wallDir = 0;
    if (standingOn == null && Mathf.Abs(movement.x) > .5f)
    {
      if (left != null) wallDir--;
      if (right != null) wallDir++;
    }
    lastClingDir = wallDir;

    if (dashes <= 0) return;
    dashes--;

    dir.Normalize();
    dir *= bulletDashForce;
    rb.velocity = dir;

    jumpCooldown = .1f;
    ReleaseControls(.1f);
  }

  void Jump()
  {
    int dir = 0;
    if (Mathf.Abs(movement.x) > .5f)
    {
      if (left != null) dir--;
      if (right != null) dir++;
    }

    if (dir == 0)
    {
      if (jumps <= 0) return;
      jumps--;
      rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
    else if (dir != lastClingDir)
    {
      rb.velocity = new Vector2(wallJumpForce.x * -dir, wallJumpForce.y);
      lastClingDir = dir;
    }

    jumpCooldown = .1f;
  }

  void FallThrough(Collider2D collider, float delay = .2f)
  {
    Physics2D.IgnoreCollision(c, collider, true);
    StartCoroutine(ResetCollision(collider, delay));
  }

  IEnumerator ResetCollision(Collider2D collider, float delay = 0f)
  {
    yield return new WaitForSeconds(delay);
    Physics2D.IgnoreCollision(c, collider, false);
  }

  void ReleaseControls(float resumeAfter)
  {
    canMove = false;
    Invoke("ResumeControls", resumeAfter);
  }

  void ResumeControls()
  {
    canMove = true;
  }
}
