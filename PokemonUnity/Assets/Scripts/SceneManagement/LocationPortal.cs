using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LocationPortal : MonoBehaviour, IPlayerTriggerable
{ 
    [SerializeField] DestinationIdentifier destionationPortal;
    [SerializeField] Transform spawnPoint;
    PlayerController player;
    Fader fader;
    public void OnPlayerTriggered(PlayerController player)
    {
        this.player = player;
        StartCoroutine(Teleport());
    }

    private void Start()
    {
       fader = FindFirstObjectByType<Fader>();
    }

    IEnumerator Teleport()
    {
        GameController.Instance.PauseGame(true);
        yield return fader.FadeIn(0.5f);
        var destPortal = FindObjectsByType<LocationPortal>(FindObjectsSortMode.None).First(x => x != this && x.destionationPortal == this.destionationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        yield return fader.FadeOut(0.5f);
        GameController.Instance.PauseGame(false);
    }

    public Transform SpawnPoint => spawnPoint;
}
