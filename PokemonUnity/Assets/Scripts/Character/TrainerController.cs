using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerController : MonoBehaviour, Interactable, ISavable
{
    [SerializeField] string trainerName;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject fov;

    bool battleLost = false;
    Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        SetFovRotation(character.Animator.DefaultDirection);
    }

    private void Update()
    {
        character.HandleUpdate();
    }

    public void Interact(Transform initiator)
    {
        character.LookTowards(initiator.position);
        if (!battleLost) {
            StartCoroutine(DialogManager.Instance.ShowDialog(dialog, () => {
                GameController.Instance.StartTrainerBattle(this);
            }));
        } else {
            StartCoroutine(DialogManager.Instance.ShowDialog(dialogAfterBattle));
        }
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));
        yield return character.Move(moveVec);

        StartCoroutine(DialogManager.Instance.ShowDialog(dialog, () => {
            GameController.Instance.StartTrainerBattle(this);
        }));
    }

    public void BattleLost()
    {
        battleLost = true;
        fov.gameObject.SetActive(false);

    }

    public void SetFovRotation(FacingDirection dir)
    {
        float angle = 0f;
        if (dir == FacingDirection.Right) {
            angle = 90f;
        } else if (dir == FacingDirection.Left) {
            angle = 270f;
        } else if (dir == FacingDirection.Up) {
            angle = 180f;
        }
        fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    public object CaptureState()
    {
        return battleLost;
    }

    public void RestoreState(object state)
    {
        battleLost = (bool)state;
        if (battleLost) {
            fov.gameObject.SetActive(false);
        }
    }

    public string Name
    {
        get { return trainerName; }
    }
    public Sprite Sprite {
        get { return sprite; }
    }
}
