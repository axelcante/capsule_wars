using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{
    [SerializeField] private GameObject m_Flag;
    [SerializeField] private Material m_FlagMaterial1;
    [SerializeField] private Material m_FlagMaterial2;

    public void SetFlagColor (Color color, string team)
    {
        switch (team) {
            case "Player1":
                m_Flag.GetComponent<Renderer>().material = m_FlagMaterial1;
                m_FlagMaterial1.SetColor("_TeamColor", color);
                break;

            case "Player2":
                m_Flag.GetComponent<Renderer>().material = m_FlagMaterial2;
                m_FlagMaterial2.SetColor("_TeamColor", color);
                break;

            default:
                break;
        }
    }
}
