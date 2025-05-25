using UnityEngine;
using TMPro;  // 👈 추가해야 함

public class ScoreUIManager : MonoBehaviour
{
    [Header("Score Texts")]
    public TextMeshProUGUI playerScoreText; // 👈 변경
    public TextMeshProUGUI aiScoreText;     // 👈 변경

    void Update()
    {
        playerScoreText.text = GameManager.Instance.playerScore.ToString();
        aiScoreText.text = GameManager.Instance.aiScore.ToString();
    }
}
