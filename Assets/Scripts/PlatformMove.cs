using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class PlatformMove : MonoBehaviour
{
  public bool moving = true;
  [Range(-1, 1)]
  public int repeat;
  private int velocity = 1;
  public bool destroyOnFinish;
  public List<PlatformPathingSegment> path = new();
  private int segmentIndex;
  private PlatformPathingSegment targetSegment;

  private Vector2 initialPosition;
  private Vector2 initialScale;
  private float initialRotation;

  private void Awake()
  {
    targetSegment = path[0].Copy();
    initialPosition = transform.position;
    initialScale = transform.localScale;
    initialRotation = transform.rotation.eulerAngles.z;
  }

  private void Update()
  {
    if (moving) Move();
  }

  private void Move()
  {
    if (velocity == 1 && targetSegment.waitStart > 0)
    {
      targetSegment.waitStart = Mathf.Max(targetSegment.waitStart - Time.deltaTime, 0);
    }
    else if (velocity == -1 && targetSegment.waitEnd > 0)
    {
      targetSegment.waitEnd = Mathf.Max(targetSegment.waitEnd - Time.deltaTime, 0);
    }
    else
    {
      if (targetSegment.subType == PlatformPathingSegment.SubType.Linear)
      {
        MoveLinear();
      }
      else if (targetSegment.subType == PlatformPathingSegment.SubType.Circular)
      {
        MoveCircular();
      }
      else
      {
        MoveSequenced();
      }
    }
  }

  private void NextSegment()
  {
    if (velocity == 1 && targetSegment.waitEnd > 0)
    {
      targetSegment.waitEnd = Mathf.Max(targetSegment.waitEnd - Time.deltaTime, 0);
    }
    else if (velocity == -1 && targetSegment.waitStart > 0)
    {
      targetSegment.waitStart = Mathf.Max(targetSegment.waitStart - Time.deltaTime, 0);
    }
    else
    {
      segmentIndex += velocity;
      if (segmentIndex < 0 || segmentIndex >= path.Count)
      {
        if (repeat == 1)
        {
          Reset();
        }
        else if (repeat == -1)
        {
          segmentIndex -= velocity;
          velocity *= -1;
          targetSegment = path[segmentIndex].Copy();
          if (velocity == -1) targetSegment.Reverse();
        }
        else
        {
          if (destroyOnFinish) Destroy(gameObject);
          else moving = false;
        }
      }
      else
      {
        targetSegment = path[segmentIndex].Copy();
        if (velocity == -1) targetSegment.Reverse();
      }
    }
  }

  private void MoveLinear()
  {
    if (targetSegment.moveTo == Vector2.zero) targetSegment.moveTo = (Vector2)transform.position + targetSegment.moveBy;
    if ((Vector2)transform.position == targetSegment.moveTo)
    {
      NextSegment();
      return;
    }
    transform.position = Vector2.MoveTowards(transform.position, targetSegment.moveTo, targetSegment.speed * Time.deltaTime);
  }

  private void MoveCircular()
  {
    if (targetSegment.rotateAround == Vector2.zero) targetSegment.rotateAround = (Vector2)transform.position + new Vector2(Mathf.Cos(targetSegment.centerAngle * Mathf.Deg2Rad), Mathf.Sin(targetSegment.centerAngle * Mathf.Deg2Rad)) * targetSegment.radius;
    if (targetSegment.rotateAroundBy == 0)
    {
      NextSegment();
      return;
    }
    float degrees = targetSegment.rotateAroundBy;
    if (targetSegment.rotateAroundBy < 0) degrees = Mathf.Clamp(degrees + targetSegment.speed * Time.deltaTime, targetSegment.rotateAroundBy, 0f);
    if (targetSegment.rotateAroundBy > 0) degrees = Mathf.Clamp(degrees - targetSegment.speed * Time.deltaTime, 0f, targetSegment.rotateAroundBy);
    Quaternion rotation = transform.rotation;
    transform.RotateAround(targetSegment.rotateAround, -Vector3.forward, targetSegment.rotateAroundBy - degrees);
    transform.rotation = rotation;
    targetSegment.rotateAroundBy = degrees;
  }

  private void MoveSequenced()
  {
    if (targetSegment.positions.Count == 0 || targetSegment.movingTo >= targetSegment.positions.Count)
    {
      NextSegment();
      return;
    }
    transform.position = Vector2.MoveTowards(transform.position, targetSegment.positions[targetSegment.movingTo], targetSegment.speed * Time.deltaTime);
    if ((Vector2)transform.position == targetSegment.positions[targetSegment.movingTo])
    {
      targetSegment.movingTo++;
    }
  }

  private void Reset()
  {
    segmentIndex = 0;
    targetSegment = path[segmentIndex].Copy();
    foreach (Transform child in transform) if (child.gameObject.tag == "Player") child.SetParent(null);
    transform.position = initialPosition;
    transform.localScale = initialScale;
    transform.rotation = Quaternion.Euler(0, 0, initialRotation);
  }
}

[System.Serializable]
public class PlatformPathingSegment
{
  public enum SubType { Linear, Circular, Sequenced };
  public SubType subType;
  public float rotateBy;
  public Vector2 scaleBy;
  public float speed = 1;
  public float waitStart;
  public float waitEnd;
  public float repeat;

  [Header("Linear")]
  public Vector2 moveBy;
  [System.NonSerialized] public Vector2 moveTo;

  [Header("Circular")]
  public float radius;
  public float centerAngle;
  public float rotateAroundBy;
  [System.NonSerialized] public Vector2 rotateAround;

  [Header("Sequenced")]
  public List<Vector2> positions = new List<Vector2>();
  [System.NonSerialized] public int movingTo = 0;

  public PlatformPathingSegment Copy()
  {
    PlatformPathingSegment copy = new PlatformPathingSegment();
    copy.subType = this.subType;
    copy.rotateBy = this.rotateBy;
    copy.scaleBy = this.scaleBy;
    copy.speed = this.speed;
    copy.waitStart = this.waitStart;
    copy.waitEnd = this.waitEnd;
    copy.repeat = this.repeat;
    copy.moveBy = this.moveBy;
    copy.moveTo = this.moveTo;
    copy.radius = this.radius;
    copy.centerAngle = this.centerAngle;
    copy.rotateAroundBy = this.rotateAroundBy;
    copy.rotateAround = this.rotateAround;
    copy.positions = new List<Vector2>(this.positions);
    copy.movingTo = this.movingTo;

    return copy;
  }

  public PlatformPathingSegment Reverse()
  {
    if (this.subType == SubType.Linear)
    {
      this.moveBy *= -1;
      return this;
    }
    else if (this.subType == SubType.Circular)
    {
      this.centerAngle -= this.rotateAroundBy;
      this.rotateAroundBy *= -1;
      return this;
    }
    else
    {
      this.positions.Reverse();
      return this;
    }
  }
}
