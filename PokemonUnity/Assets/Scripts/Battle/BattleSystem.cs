using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver }
public enum BattleAction { Move, SwitchPokemon, UseItem, Run}

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;

    public event Action<bool> OnBattleOver;
    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;
    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;
    int escapeAttempts;
    MoveBase moveToLearn;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon)
    {
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;
        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;
        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;
        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle) {
            //wild pokemon battle
            playerUnit.Setup(playerParty.GetHealthyPokemon());
            enemyUnit.Setup(wildPokemon);
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
            yield return dialogBox.TypeDialog($"A wild {enemyUnit.Pokemon.Base.Name} appeared !");
        } else {
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);
            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;
            yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle !");

            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyPokemon.Base.Name} !");

            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {playerPokemon.Base.Name} !");
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        }
        escapeAttempts = 0;
        partyScreen.Init();
        ActionSelection();
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an action :");
        dialogBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection() {
        state =BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to switch Pokemon ?");
        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;
        state = BattleState.MoveToForget;
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move) {
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority) {
                playerGoesFirst = false;
            } else if (enemyMovePriority == playerMovePriority) {
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;
            }

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver)
                yield break;

            if (secondPokemon.HP > 0) {
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver)
                yield break;
            }
        } else {
            if (playerAction ==  BattleAction.SwitchPokemon) {
                var selectedPokemon = playerParty.Pokemons[currentMember];
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
            } else if (playerAction == BattleAction.UseItem) {
                dialogBox.EnableActionSelector(false);
                yield return ThrowPokeball();
            } else if (playerAction == BattleAction.Run) {
                yield return TryToEscape();
            }

            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver)
                yield break;
        }
        if (state != BattleState.BattleOver) {
            ActionSelection();
        }
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();
        if (!canRunMove) {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);


        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name} !");
        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon)) {
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status) {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            } else {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }
            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0) {
                foreach (var secondary in move.Base.Secondaries) {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance) {
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                    }
                }
            }
            if (targetUnit.Pokemon.HP <= 0) {
                yield return HandlePokemonFainted(targetUnit);
            }
        } else {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed !");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
    {
        if (effects.Boosts != null) {
            if (moveTarget == MoveTarget.Self) {
                source.ApplyBoosts(effects.Boosts);
            } else {
                target.ApplyBoosts(effects.Boosts);
            }
        }
        if (effects.Status != ConditionID.none) {
            target.SetStatus(effects.Status);
        }
        if (effects.VolatileStatus != ConditionID.none) {
            target.SetVolatileStatus(effects.VolatileStatus);
        }
        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);

        if (effects.Status != ConditionID.none) {
            target.SetStatus(effects.Status);
        }
        if (effects.VolatileStatus != ConditionID.none) {
            target.SetVolatileStatus(effects.VolatileStatus);
        }
        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }
    
    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver)
            yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Pokemon.HP <= 0) {
            yield return HandlePokemonFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
    {
        if (move.Base.AlwaysHits) {
            return true;
        }
        float moveAccuracy = move.Base.Accuracy;
        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];
        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0) {
            moveAccuracy *= boostValues[accuracy];
        } else {
            moveAccuracy /= boostValues[-accuracy];
        }
        if (evasion > 0) {
            moveAccuracy /= boostValues[evasion];
        } else {
            moveAccuracy *= boostValues[-evasion];
        }
        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0) {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.Base.Name} fainted !");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);
        if (!faintedUnit.IsPlayerUnit) {
            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;
            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Pokemon.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp !");
            yield return playerUnit.Hud.SetExpSmooth();

            while (playerUnit.Pokemon.CheckForLevelUp()) {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} grew to level {playerUnit.Pokemon.Level} !");
                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                if (newMove != null) {
                    if (playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumOffMoves)
                    {
                        playerUnit.Pokemon.LearnMove(newMove);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}");
                        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Base.Name}");
                        yield return dialogBox.TypeDialog($"But it cannot learn more than {PokemonBase.MaxNumOffMoves} moves");
                        yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(1f);
                    }
                }
                yield return playerUnit.Hud.SetExpSmooth(true);
            }
            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit) {
            var nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon != null) {
                OpenPartyScreen();
            } else {
                BattleOver(false);
            }
        } else {
            if (!isTrainerBattle) {
                BattleOver(true);
            } else {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon != null) {
                    StartCoroutine(AboutToUse(nextPokemon));
                } else {
                    BattleOver(true);
                }
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit !");
        if (damageDetails.Type > 1f)
            yield return dialogBox.TypeDialog("It's super effective !");
        else if (damageDetails.Type < 1f)
            yield return dialogBox.TypeDialog("It's not very effective...");
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection) {
            HandleActionSelection();
        } else if (state == BattleState.MoveSelection) {
            HandleMoveSelection();
        } else if (state == BattleState.PartyScreen) {
            HandlePartySelection();
        } else if (state == BattleState.AboutToUse) {
            HandleAboutToUse();
        } else if (state == BattleState.MoveToForget) {
            Action<int> onMoveSelected = (moveIndex) => {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == PokemonBase.MaxNumOffMoves) {
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} did not learn {moveToLearn.Name}"));
                } else {
                    var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));
                    playerUnit.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
                }
                moveToLearn = null;
                state = BattleState.RunningTurn;
            };
            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            currentAction++;
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            currentAction--;
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            currentAction += 2;
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            currentAction -= 2;
        }
        currentAction = Mathf.Clamp(currentAction, 0, 3);
        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.Return)) {
            if (currentAction == 0) {
                // Fight
                MoveSelection();
            } else if (currentAction == 1) {
                StartCoroutine(RunTurns(BattleAction.UseItem));
            } else if (currentAction == 2) {
                // Pokemons
                prevState = state;
                OpenPartyScreen();
            } else if (currentAction == 3) {
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            currentMove++;
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            currentMove--;
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            currentMove += 2;
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            currentMove -= 2;
        }
        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);
        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);
        if (Input.GetKeyDown(KeyCode.Return)) {
            var move = playerUnit.Pokemon.Moves[currentMove];
            if (move.PP == 0) {
                return;
            }
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        } else if (Input.GetKeyDown(KeyCode.X)) {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            currentMember++;
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            currentMember--;
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            currentMember += 2;
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            currentMember -= 2;
        }
        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);
        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Return)) {
            var selectedMember = playerParty.Pokemons[currentMember];
            if (selectedMember.HP <= 0) {
                partyScreen.SetMessageText("You can't switch a fainted Pokemon !");
                return;
            }
            if (selectedMember == playerUnit.Pokemon) {
                partyScreen.SetMessageText("You already have this Pokemon out !");
                return;
            }

            partyScreen.gameObject.SetActive(false);
            if (prevState == BattleState.ActionSelection) {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            } else {
                state = BattleState.Busy;
                StartCoroutine(SwitchPokemon(selectedMember));
            }
        } else if (Input.GetKeyDown(KeyCode.X)) {
            if (playerUnit.Pokemon.HP <= 0) {
                partyScreen.SetMessageText("You have to choose a pokemon to continue !");
                return;
            }
            partyScreen.gameObject.SetActive(false);
            if ( prevState == BattleState.AboutToUse) {
                prevState = null;
                StartCoroutine(SendNextTrainerPokemon());
            } else {
                ActionSelection();
            }
        }
    }

    void HandleAboutToUse()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)) {
            aboutToUseChoice = !aboutToUseChoice;
        }
        dialogBox.UpdateChoiceBox(aboutToUseChoice);
        if (Input.GetKeyDown(KeyCode.Return)) {
            dialogBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true) {
                prevState = BattleState.AboutToUse;
                OpenPartyScreen();
            } else {
                StartCoroutine(SendNextTrainerPokemon());
            }
        } else if (Input.GetKeyDown(KeyCode.X)) {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SwitchPokemon(Pokemon newPokemon)
    {
        if (playerUnit.Pokemon.HP > 0) {
            yield return dialogBox.TypeDialog($"Come Back {playerUnit.Pokemon.Base.Name} !");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }
        playerUnit.Setup(newPokemon);
        dialogBox.SetMoveNames(newPokemon.Moves);
        currentMove = 0;
        yield return dialogBox.TypeDialog($"Go {newPokemon.Base.Name} !");
        if (prevState == null) {

            state = BattleState.RunningTurn;
        } else if (prevState == BattleState.AboutToUse) {
            prevState = null;
            StartCoroutine(SendNextTrainerPokemon());
        }
        state = BattleState.RunningTurn;
    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;
        var nextPokemon = trainerParty.GetHealthyPokemon();
        enemyUnit.Setup(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name} !");
        state = BattleState.RunningTurn;
    }

    IEnumerator ThrowPokeball()
    {
        state = BattleState.Busy;

        if (isTrainerBattle) {
            yield return dialogBox.TypeDialog($"You can't steal the trainers pokemon !");
            state = BattleState.ActionSelection;
            yield break;
        }

        yield return dialogBox.TypeDialog($"{player.Name} used Pokeball !");
        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();

        // Animations pokeball
        yield return pokeball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); i++) {
            yield return new WaitForSeconds(0.5f);
            yield return pokeball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }
        if (shakeCount == 4) {
            //pokemon Caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was caught !");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();
            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added to your party !");
            Destroy(pokeball);
            BattleOver(true);
        } else {
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();
            if (shakeCount < 2) {
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free !");
            } else {
                yield return dialogBox.TypeDialog($"Almost caught it");
            }
            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }

    int TryToCatchPokemon(Pokemon pokemon)
    {
        float a = (3 * pokemon.MaxHP -2 * pokemon.HP) * pokemon.Base.CatchRate * ConditionsDB.GetStatusBonus(pokemon.Status) / (3 * pokemon.MaxHP);
        if (a >= 255) {
            return 4;
        }
        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));
        int shakeCount = 0;
        while (shakeCount < 4) {
            if (UnityEngine.Random.Range(0, 65535) >= b) {
                break;
            }
            shakeCount++;
        }
        return shakeCount;
    }

    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;
        if (isTrainerBattle) {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles !");
            state = BattleState.ActionSelection;
            yield break;
        }
        escapeAttempts++;
        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;
        if (enemySpeed < playerSpeed) {
            yield return dialogBox.TypeDialog($"Run away selfy");
            BattleOver(true);
        } else {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;
            if (UnityEngine.Random.Range(0, 256) < f) {
                yield return dialogBox.TypeDialog($"Run away selfy");
                BattleOver(true);
            } else {
                yield return dialogBox.TypeDialog($"Can't escape !");
                state = BattleState.RunningTurn;
            }
        }
    }
}
