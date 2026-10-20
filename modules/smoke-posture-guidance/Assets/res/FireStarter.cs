using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireStarter : MonoBehaviour
{
    public Flammable targetFlammable;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (targetFlammable != null)
            {
                targetFlammable.Ignite();
            }
        }
    }
}