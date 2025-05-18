using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions) {
            var conditionId = kvp.Key;
            var condition = kvp.Value;
            condition.Id = conditionId;
        }
    }
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Id = ConditionID.psn,
                Name = "Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Pokemon pokemon) => {
                    pokemon.UpdateHP(pokemon.MaxHP / 8);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by poison !");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Id = ConditionID.brn,
                Name = "Burn",
                StartMessage = "has been burned",
                OnAfterTurn = (Pokemon pokemon) => {
                    pokemon.UpdateHP(pokemon.MaxHP / 16);
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is hurt by burn !");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Id = ConditionID.par,
                Name = "Paralyzed",
                StartMessage = "has been Paralyzed",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1) {
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is paralyzed and can't move !");
                        return false;
                    }
                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Id = ConditionID.frz,
                Name = "Freeze",
                StartMessage = "has been frozen",
                OnBeforeMove = (Pokemon pokemon) => {
                    if (Random.Range(1, 5) == 1) {
                        pokemon.CureStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is not frozen anymore !");
                        return true;
                    }
                    return false;
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Id = ConditionID.slp,
                Name = "Sleep",
                StartMessage = "has been asleep",
                OnStart = (Pokemon pokemon) => {
                    pokemon.StatusTime = Random.Range(1, 4);
                    Debug.Log($"Sleep time: {pokemon.StatusTime}");
            },
            OnBeforeMove = (Pokemon pokemon) => {
                if (pokemon.StatusTime == 0) {
                    pokemon.CureStatus();
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} woke up !");
                    return true;
                }
                pokemon.StatusTime--;
                pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is asleep !");
                return false;
                }
            } 
        },
        {
            ConditionID.confusion,
            new Condition()
            {
                Id = ConditionID.confusion,
                Name = "Confusion",
                StartMessage = "is confused",
                OnStart = (Pokemon pokemon) => {
                    pokemon.VolatileStatusTime = Random.Range(1, 5);
                    Debug.Log($"Confusion time: {pokemon.VolatileStatusTime}");
                },
                OnBeforeMove = (Pokemon pokemon) => {
                    if (pokemon.VolatileStatusTime <= 0) {
                        pokemon.CureVolatileStatus();
                        pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} kicked out of confusion !");
                        return true;
                    }
                    pokemon.VolatileStatusTime--;
                    if (Random.Range(1, 3) == 1) {
                        return true;
                    }
                    pokemon.StatusChanges.Enqueue($"{pokemon.Base.Name} is confused !");
                    pokemon.UpdateHP(pokemon.MaxHP / 8);
                    pokemon.StatusChanges.Enqueue($"It hurt itself in confusion !");
                    return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null) {
            return 1f;
        } else if (condition.Id == ConditionID.slp || condition.Id == ConditionID.frz) {
            return 2f;
        } else if (condition.Id == ConditionID.par || condition.Id == ConditionID.psn || condition.Id == ConditionID.brn) {
            return 1.5f;
        }
        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, par, frz, slp,
    confusion
}