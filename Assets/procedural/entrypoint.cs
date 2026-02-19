using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class entrypoint : MonoBehaviour
{
    private bool isOccupied = false;
    public void SetOccupied(bool value = true) { isOccupied = value; }
    public bool IsOccupied() => isOccupied;
}
