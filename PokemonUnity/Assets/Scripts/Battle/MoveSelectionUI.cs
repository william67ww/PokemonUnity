using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MoveSelectionUI : MonoBehaviour
{
    [SerializeField] List<Text> moveTexts;
    [SerializeField] Color highLightedColor;
    int currentSelection = 0;

    public void SetMoveData(List<MoveBase> currentMoves, MoveBase newMove)
    {
        for (int i = 0; i < currentMoves.Count; i++)
        {
            moveTexts[i].text = currentMoves[i].Name;
        }
        moveTexts[currentMoves.Count].text = newMove.Name;
    }

    public void HandleMoveSelection(Action<int> onSelected)
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSelection++;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSelection--;
        }
        currentSelection = Mathf.Clamp(currentSelection, 0, PokemonBase.MaxNumOffMoves);
        UpdateMoveSelection(currentSelection);
        if (Input.GetKeyDown(KeyCode.Return))
        {
            onSelected?.Invoke(currentSelection);
        }
    }

    public void UpdateMoveSelection(int selection)
    {
        for (int i = 0; i < PokemonBase.MaxNumOffMoves + 1; i++) {
            if (i == selection) {
                moveTexts[i].color = highLightedColor;
            } else {
                moveTexts[i].color = Color.black;   
            }
        }
    }
}
