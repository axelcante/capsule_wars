using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightButton : MonoBehaviour
{
    public void OnClick ()
    {
        UIManager.Instance.UIButtonFight();
    }
}
