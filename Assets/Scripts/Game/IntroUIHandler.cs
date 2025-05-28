using UnityEngine;

public class IntroUIHandler : MonoBehaviour
{
    public CameraManager cameraManager;
    
    public GameObject modeText1;
   public GameObject modeText2;

    private void Start()
    {
        // 시작 시 비활성화 (선택사항)
        if (modeText1 != null) modeText1.SetActive(false);
        if (modeText2 != null) modeText2.SetActive(false);
    }

    // 애니메이션 이벤트에서 호출됨
    public void OnIntro_1()
    {
        Debug.Log("🎬 Intro Animation Complete!");

        // 텍스트 활성화
        if (modeText1 != null) modeText1.SetActive(true);
        if (modeText2 != null) modeText2.SetActive(true);

    }
}
