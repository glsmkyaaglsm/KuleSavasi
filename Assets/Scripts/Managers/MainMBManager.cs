using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMBManager : MonoBehaviour 
{
    [Header("UI")]
    public GameObject settingPanel;
    public GameObject profilPanel;
    public GameObject startMenuPanel;
    public GameObject iceTowersPanel;
    public GameObject airPlaneTowersPanel;
    public GameObject mainMenuPanel;

    private void Awake()
    {
        settingPanel?.SetActive(false);
        profilPanel?.SetActive(false);
        startMenuPanel?.SetActive(false);
        iceTowersPanel?.SetActive(false);
        airPlaneTowersPanel?.SetActive(false);

    }
    public void StartButton()
    {
        startMenuPanel?.SetActive(true);
        settingPanel?.SetActive(false);
        profilPanel?.SetActive(false);
        VibrationManager.Vibrate(50);

    }
    public void Exit()
    {
        startMenuPanel?.SetActive(false);
        settingPanel?.SetActive(false);
        profilPanel?.SetActive(false);
        VibrationManager.Vibrate(50);
    }
    public void ProfilButton()
    {
        profilPanel?.SetActive(true);
        settingPanel?.SetActive(false);
        startMenuPanel?.SetActive(false);
        VibrationManager.Vibrate(50);
    }
    public void SettingButton()
    {
        settingPanel?.SetActive(true);
        profilPanel?.SetActive(false);
        startMenuPanel?.SetActive(false);
        VibrationManager.Vibrate(50);
    }
    public void IceTowerStart()
    {
        settingPanel?.SetActive(false);
        profilPanel?.SetActive(false);
        startMenuPanel?.SetActive(false);
        airPlaneTowersPanel?.SetActive(false);
        mainMenuPanel?.SetActive(false);
        iceTowersPanel?.SetActive(true);
        VibrationManager.Vibrate(50);
    }
    public void AirPlaneTowerStart()
    {
        settingPanel?.SetActive(false);
        profilPanel?.SetActive(false);
        startMenuPanel?.SetActive(false);
        iceTowersPanel?.SetActive(false);
        mainMenuPanel?.SetActive(false);
        airPlaneTowersPanel?.SetActive(true);
        VibrationManager.Vibrate(50);
    }


}
