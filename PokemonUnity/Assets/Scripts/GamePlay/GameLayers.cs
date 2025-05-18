using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLayers : MonoBehaviour
{
    [SerializeField] LayerMask solidObjectLayer;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask grassLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask fovLayer;
    [SerializeField] LayerMask portalLayer;

    public static GameLayers i { get; set; }

    private void Awake()
    {
        i = this;
    }

    public LayerMask SolidLayer
    {
        get
        {
            return solidObjectLayer;
        }
    }

    public LayerMask InteractableLayer
    {
        get
        {
            return interactableLayer;
        }
    }

    public LayerMask GrassLayer
    {
        get
        {
            return grassLayer;
        }
    }

    public LayerMask PlayerLayer
    {
        get
        {
            return playerLayer;
        }
    }
    public LayerMask FovLayer
    {
        get
        {
            return fovLayer;
        }
    }
    public LayerMask PortalLayer
    {
        get
        {
            return portalLayer;
        }
    }

    public LayerMask TriggerableLayers
    {
        get
        {
            return grassLayer | fovLayer | portalLayer;
        }
    }
}
