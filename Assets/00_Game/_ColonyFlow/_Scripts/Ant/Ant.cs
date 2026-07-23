using UnityEngine;

public class Ant : MonoBehaviour
{
    public string colorHex;
    public float moveSpeed = 3f;
    public float turnSpeed = 540f;

    public void SetColor(string hex)
    {
        colorHex = hex;
    }
}
