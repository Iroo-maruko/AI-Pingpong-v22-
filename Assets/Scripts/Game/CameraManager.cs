using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public Camera introCamera;
    public Camera mainCamera1; // MeVsMe 카메라
    public Camera mainCamera2; // MeVsAI 카메라

    [Header("Game Object Groups")]
    public GameObject group1; // 1번 모드 전체 그룹
    public GameObject group2; // 2번 모드 전체 그룹

    public GameObject modeSelectionUI;

    [Header("Game Managers")]
    public GameObject gameManager1;
    public GameObject gameManager2;

    private bool modeChosen = false;

    private void Start()
    {
        // 초기 상태 설정
        introCamera.enabled = true;
        mainCamera1.enabled = false;
        mainCamera2.enabled = false;

        introCamera.gameObject.SetActive(true);
        mainCamera1.gameObject.SetActive(true);
        mainCamera2.gameObject.SetActive(true);

        introCamera.tag = "MainCamera";
        mainCamera1.tag = "Untagged";
        mainCamera2.tag = "Untagged";

        gameManager1.SetActive(false);
        gameManager2.SetActive(false);
        group1.SetActive(false);
        group2.SetActive(false);
    }

    private void Update()
    {
        if (modeChosen) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            GameModeManager.Instance?.SetMeVsMe();
            modeChosen = true;
            OnModeSelected();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            GameModeManager.Instance?.SetMeVsAI();
            modeChosen = true;
            OnModeSelected();
        }
    }

    public void OnModeSelected()
    {
        StartCoroutine(SwitchToMainCamera());
    }

    private IEnumerator SwitchToMainCamera()
    {
        yield return new WaitForSeconds(1.5f);

        introCamera.enabled = false;
        introCamera.tag = "Untagged";

        switch (GameModeManager.Instance.currentMode)
        {
            case GameMode.MeVsMe:
                mainCamera1.enabled = true;
                mainCamera2.enabled = false;

                mainCamera1.tag = "MainCamera";
                mainCamera2.tag = "Untagged";

                gameManager1.SetActive(true);
                group1.SetActive(true);
                group2.SetActive(false);
                Debug.Log("✅ MeVsMe 모드 시작");
                break;

            case GameMode.MeVsAI:
                mainCamera1.enabled = false;
                mainCamera2.enabled = true;

                mainCamera1.tag = "Untagged";
                mainCamera2.tag = "MainCamera";

                gameManager2.SetActive(true);
                group2.SetActive(true);
                group1.SetActive(false);
                Debug.Log("✅ MeVsAI 모드 시작");
                break;

            default:
                Debug.LogWarning("⚠️ Game mode not set!");
                break;
        }

        modeSelectionUI.SetActive(false);
        Debug.Log("🎥 Camera active: " + Camera.main?.name);
    }
}
