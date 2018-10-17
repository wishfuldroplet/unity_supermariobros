﻿using UnityEngine;
using System;
using System.Collections.Generic;
using WishfulDroplet;
using WishfulDroplet.Extensions;


public class Character2D : MonoBehaviour {
    public Vector2                                          FaceAxis { get { return faceAxis; } }
    public Vector2                                          MaxVelocity { get { return maxVelocity; } }
    public Vector2                                          Velocity { get { return thisRigidbody2D.velocity; } }
    public bool                                             IsGrounded { get { return isGrounded; } }
    public bool                                             IsChangingDirection {
        get {
            return (thisRigidbody2D.velocity.x > 0f && IsFacing(Direction.Left)) ||
                   (thisRigidbody2D.velocity.x < 0f && IsFacing(Direction.Right));
        }
    }

    [SerializeField] private Vector2                        maxVelocity = new Vector2(5f, 8f);
    [SerializeField] private bool                           isUpdateFaceAxisOnlyOnGround = true;
    [SerializeField] private DirectionalBoxCast2D           directionalBoxCast = new DirectionalBoxCast2D();
    [SerializeField] private int                            maxHitBufferSize = 20;
    [SerializeField] private List<Collider2D>               boxCastMask = new List<Collider2D>();

    [Header("Debug")]
    [SerializeField] private List<Collider2D>               touchedColliders = new List<Collider2D>();
    [SerializeField] private Vector2                        moveDirection;
    [SerializeField] private Vector2                        faceAxis;
    [SerializeField] private bool                           isGrounded;

    [Header("References")]
    [SerializeField] private Transform                      thisTransform;
    [SerializeField] private Rigidbody2D                    thisRigidbody2D;
    [SerializeField] private BoxCollider2D                  thisBoxCollider2D;


    public bool IsColliding(Direction direction, Collider2D collider = null) {
        return directionalBoxCast.IsHit(direction, collider);
    }

    public bool IsMoving(Direction direction) {
        switch(direction) {
            case Direction.Up: return thisRigidbody2D.velocity.y > 0f;
            case Direction.Down: return thisRigidbody2D.velocity.y < 0f;
            case Direction.Left: return thisRigidbody2D.velocity.x < 0f;
            case Direction.Right: return thisRigidbody2D.velocity.x > 0f;
            case Direction.Any: return thisRigidbody2D.velocity != Vector2.zero;
            default: return false;
        }
    }

    public bool IsFacing(Direction direction) {
        switch(direction) {
            case Direction.Up: return faceAxis.y > 0f;
            case Direction.Down: return faceAxis.y < 0f;
            case Direction.Left: return faceAxis.x < 0f;
            case Direction.Right: return faceAxis.x > 0f;
            default: return true;
        }
    }

    public void Move(Vector2 direction) {
        // This could be optimized
        if(IsColliding(Direction.Up) && direction.y > 0) {
            direction.y = 0f;
        }
        if(IsColliding(Direction.Down) && direction.y < 0) {
            direction.y = 0f;
        }
        if(IsColliding(Direction.Left) && direction.x < 0) {
            direction.x = 0f;
        }
        if(IsColliding(Direction.Right) && direction.x > 0) {
            direction.x = 0f;
        }

        moveDirection += direction;
    }

    public void SetVelocity(Vector2 velocity) {
        thisRigidbody2D.velocity = velocity;
    }

    private void ClampVelocity() {
        if(Mathf.Abs(thisRigidbody2D.velocity.x) > maxVelocity.x) {
            thisRigidbody2D.velocity = new Vector2(maxVelocity.x * Mathf.Sign(thisRigidbody2D.velocity.x), thisRigidbody2D.velocity.y);
        }

        if(Mathf.Abs(thisRigidbody2D.velocity.y) > maxVelocity.y) {
            thisRigidbody2D.velocity = new Vector2(thisRigidbody2D.velocity.x, maxVelocity.y * Mathf.Sign(thisRigidbody2D.velocity.y));
        }
    }

    private void Reset() {
        thisTransform = this.GetComponent<Transform>();
        thisRigidbody2D = gameObject.AddOrGetComponent<Rigidbody2D>();
        thisBoxCollider2D = gameObject.AddOrGetComponent<BoxCollider2D>();

        directionalBoxCast.BoxInfos = new DirectionalBoxCast2D.BoxCastInfo[4] {
            new DirectionalBoxCast2D.BoxCastInfo { Direction = Direction.Up, SizeMultiplier = .02f },
            new DirectionalBoxCast2D.BoxCastInfo { Direction = Direction.Down, SizeMultiplier = .02f },
            new DirectionalBoxCast2D.BoxCastInfo { Direction = Direction.Left, SizeMultiplier = .02f },
            new DirectionalBoxCast2D.BoxCastInfo { Direction = Direction.Right, SizeMultiplier = .02f },
        };
        directionalBoxCast.ReferenceCollider = thisBoxCollider2D;

        boxCastMask = new List<Collider2D>(GetComponentsInChildren<Collider2D>(true));
    }

    private void Awake() {
        directionalBoxCast.SetHitBufferSize(maxHitBufferSize);
    }

    private void Update() {
        // Bugs: thisRigidbody.velocity.y is having some funny values for some reason
        //       when walking or sprinting. Investigate this when you have some time
        //       For now we HOTFIX it by doing a "||" operator instead of an "&&" 
        //       operator
        // FIXED: It was not the code, but the composite collider issues, it seems to be 
        //        a Unity bug where the vertex snapping leaves very small gaps that could
        //        screw around with collisions
        isGrounded = IsColliding(Direction.Down) && thisRigidbody2D.velocity.y == 0f;
    }

    private void FixedUpdate() {
        if(moveDirection != Vector2.zero) {
            if(isUpdateFaceAxisOnlyOnGround && isGrounded) {
                faceAxis.x = moveDirection.x < 0f ? -1f : moveDirection.x > 0f ? 1f : faceAxis.x;
                faceAxis.y = moveDirection.y < 0f ? -1f : moveDirection.y > 0f ? 1f : faceAxis.y;
            }

            thisRigidbody2D.AddForce(moveDirection, ForceMode2D.Force);
            //thisRigidbody2D.velocity += moveDirection;
            moveDirection = Vector2.zero;
        }

        ClampVelocity();
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if(!touchedColliders.Contains(collision.collider)) {
            touchedColliders.Add(collision.collider);
        }
        directionalBoxCast.GetHits(touchedColliders, boxCastMask);
    }

    private void OnCollisionExit2D(Collision2D collision) {
        touchedColliders.Remove(collision.collider);
        directionalBoxCast.RemoveHit(collision.collider);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if(directionalBoxCast.BoxInfos != null) {
            for(int i = 0; i < directionalBoxCast.BoxInfos.Length; i++) {
                Gizmos.DrawWireCube(directionalBoxCast.BoxInfos[i].Origin,
                                    directionalBoxCast.BoxInfos[i].Size);
            }
        }
    }
}
