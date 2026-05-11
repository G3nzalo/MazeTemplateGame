using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPepe : MonoBehaviour
{
    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.renderQueue = 1000; // Se renderiza al fondo

    }
}
