using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] List<Sprite> walkDownFrames;
    [SerializeField] List<Sprite> walkUpFrames;
    [SerializeField] List<Sprite> walkLeftFrames;
    [SerializeField] List<Sprite> walkRightFrames;
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;

    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }

    SpriteAnimator walkDownAnim;
    SpriteAnimator walkUpAnim;
    SpriteAnimator walkLeftAnim;
    SpriteAnimator walkRightAnim;
    SpriteAnimator currentAnim;
    bool wasPreviouslyMoving;

    SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        walkDownAnim = new SpriteAnimator(walkDownFrames, spriteRenderer);
        walkUpAnim = new SpriteAnimator(walkUpFrames, spriteRenderer);
        walkLeftAnim = new SpriteAnimator(walkLeftFrames, spriteRenderer);
        walkRightAnim = new SpriteAnimator(walkRightFrames, spriteRenderer);
        SetFacingDirection(defaultDirection);

        currentAnim = walkDownAnim;
    }

    private void Update()
    {
        var prevAnim = currentAnim;

        if (MoveX == 1) {
            currentAnim = walkRightAnim;
        } else if (MoveX == -1) {
            currentAnim = walkLeftAnim;
        } else if (MoveY == 1) {
            currentAnim = walkUpAnim;
        } else if (MoveY == -1) {
            currentAnim = walkDownAnim;
        }
        if (currentAnim != prevAnim || IsMoving != wasPreviouslyMoving) {
            currentAnim.Start();
        }
        if (IsMoving) {
            currentAnim.HandleUpdate();
        } else {
            spriteRenderer.sprite = currentAnim.Frames[0];
        }
        wasPreviouslyMoving = IsMoving;
    }

    public void SetFacingDirection(FacingDirection dir)
    {
        if (dir == FacingDirection.Right) {
            MoveX = 1;
        } else if (dir == FacingDirection.Left) {
            MoveX = -1;
        } else if (dir == FacingDirection.Up) {
            MoveY = 1;
        } else if (dir == FacingDirection.Down) {
            MoveY = -1;
        }
    }
    public FacingDirection DefaultDirection {
        get => defaultDirection;
    }
}

public enum FacingDirection { Up, Down, Left, Right }
