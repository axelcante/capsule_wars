using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject m_TitleContainer;
    [SerializeField] private GameObject m_UnitButtonsContainer;
    [SerializeField] private GameObject m_Team1Button;
    [SerializeField] private GameObject m_Team2Button;
    [SerializeField] private TMP_Text m_UnitCount1;
    [SerializeField] private TMP_Text m_UnitCount2;
    [SerializeField] private GameObject m_ResetButton;

    #region SINGLETON DECLARATION

    // Singleton implementation pattern
    private static UIManager m_instance;
    public static UIManager Instance
    {
        get { return m_instance; }
    }

    #endregion SINGLETON DECLARATION
    
    // Awake is called when this script is loaded
    private void Awake ()
    {
        // Singleton declaration
        if (m_instance != null && m_instance != this)
            Destroy(gameObject);
        else
            m_instance = this;
    }

    // Start is called before the first frame
    private void Start ()
    {
        RefreshUI();
    }

    // Refresh the UI variables and display
    public void RefreshUI ()
    {
        int remainingUnits1 = GameManager.Instance.GetRemaningUnitsToPlace(0);
        int remainingUnits2 = GameManager.Instance.GetRemaningUnitsToPlace(1);

        // Hide button if all units have been placed, or update the displayed count
        if (remainingUnits1 <= 0)
            m_Team1Button.SetActive(false);
        else
            m_UnitCount1.text = remainingUnits1.ToString();

        // Hide button if all units have been placed, or update the displayed count
        if (remainingUnits2 <= 0)
            m_Team2Button.SetActive(false);
        else
            m_UnitCount2.text = remainingUnits2.ToString();
    }

    // Move from title screen to units screen
    public void UIButtonPlay ()
    {
        m_TitleContainer.SetActive(false);
        m_UnitButtonsContainer.SetActive(true);
        m_ResetButton.SetActive(false);
        RefreshUI();
    }



    // Mark the game as started
    public void UIButtonFight ()
    {
        m_TitleContainer.SetActive(false);
        m_UnitButtonsContainer.SetActive(false);
        m_ResetButton.SetActive(true);
        GameManager.Instance.Play();
    }



    // Reset UI back to title screen
    public void ResetUI ()
    {
        m_TitleContainer.SetActive(true);
        m_UnitButtonsContainer.SetActive(false);
        m_ResetButton.SetActive(false);
    }
}
