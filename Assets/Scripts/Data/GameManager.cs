using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public BallController ball;
    public AIAgent aiAgent;

    [Header("Score Settings")]
    public int playerScore = 0;
    public int aiScore = 0;
    public int pointsToWin = 11;

    public string serveBy { get; private set; } = "None";

    private string currentHitter = "None";
    private string lastTableBounce = "None";
    private bool isServe = true;
    private int serveBounceCount = 0;
    private bool pointAwardedThisRally = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        BallController.OnPaddleHit += HandlePaddleHit;
        BallController.OnTableBounce += HandleTableBounce;
        BallController.OnOutOfBounds += HandleOutOfBounds;
    }

    private void OnDisable()
    {
        BallController.OnPaddleHit -= HandlePaddleHit;
        BallController.OnTableBounce -= HandleTableBounce;
        BallController.OnOutOfBounds -= HandleOutOfBounds;
    }

    private void HandlePaddleHit(string hitter)
    {
        if (pointAwardedThisRally) return;

        currentHitter = hitter;
        Debug.Log($"🎯 [HIT] currentHitter = {currentHitter}");

        if (isServe)
            isServe = false;

        serveBounceCount = 0;
    }

    private void HandleTableBounce(string table)
    {
        if (pointAwardedThisRally) return;

        if (isServe)
        {
            serveBounceCount++;

            if (serveBounceCount == 1)
            {
                if (!table.Contains(serveBy))
                {
                    AwardPoint(Opponent(serveBy), "잘못된 서브 바운스");
                    return;
                }
            }
            else if (serveBounceCount == 2)
            {
                isServe = false;
            }

            return;
        }

        if (table.Contains(currentHitter))
        {
            AwardPoint(Opponent(currentHitter), "자기쪽 테이블에 바운스");
            return;
        }

        if (currentHitter == "AI" && table == "PlayerTable")
        {
            aiAgent?.AddReward(0.3f);
            Debug.Log("🏅 AI rewarded for successful return to Player side");
        }

        lastTableBounce = table;
    }

    private void HandleOutOfBounds(string tableBeforeExit)
    {
        if (pointAwardedThisRally) return;

        if (tableBeforeExit.Contains(currentHitter))
            AwardPoint(Opponent(currentHitter), "공을 쳐서 바로 아웃");
        else
            AwardPoint(currentHitter, "상대가 리턴 못함");
    }

    private void AwardPoint(string winner, string reason)
    {
        pointAwardedThisRally = true;

        if (winner == "Player") playerScore++;
        else if (winner == "AI") aiScore++;

        Debug.Log($"✅ {winner} 점수 획득: {reason} | 점수: Player {playerScore} - AI {aiScore}");

        if (winner == "AI")
        {
            aiAgent?.AddReward(+1f);
            aiAgent?.EndEpisode();
        }
        else
        {
            aiAgent?.AddReward(-0.2f);
        }

        if (playerScore >= pointsToWin || aiScore >= pointsToWin)
        {
            Debug.Log("🏁 게임 종료: " + (playerScore >= pointsToWin ? "Player 승리" : "AI 승리"));
            playerScore = 0;
            aiScore = 0;
        }

        StartCoroutine(HandlePostPointRoutine(winner));
    }

    private IEnumerator HandlePostPointRoutine(string nextServer)
    {
        yield return new WaitForSeconds(1f);
        PrepareNextServe(nextServer);
        yield return StartCoroutine(ball.ResetPosition());
        ball.LaunchBall(nextServer);
    }

    private void PrepareNextServe(string nextServer)
    {
        currentHitter = nextServer;
        serveBy = nextServer;
        lastTableBounce = "None";
        isServe = true;
        serveBounceCount = 0;
        pointAwardedThisRally = false;
    }

    private string Opponent(string player) => player == "Player" ? "AI" : "Player";
}
