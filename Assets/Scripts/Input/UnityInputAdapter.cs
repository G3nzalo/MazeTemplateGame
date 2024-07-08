using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class UnityInputAdapter : IInput
{
    public float GetDirectionHorizontal()
    {
        var horizontal = UnityEngine.Input.GetAxis("Horizontal");
        return horizontal;
    }

    public float GetDirectionVertical()
    {
        var vertical = UnityEngine.Input.GetAxis("Vertical");
        return vertical;
    }

}