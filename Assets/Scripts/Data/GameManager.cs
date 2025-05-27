using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Objects")]
    public BallController ball;
    public Transform centerPoint;
    public LearningLogger logger;
    public Transform aiPaddle;
    public Transform playerPaddle;

    [Header("Score Settings")]
    public int playerScore = 0;
    public int aiScore = 0;
    public int pointsToWin = 11;

    private int bounceCount = 0;
    private bool isPointScored = false;
    private int totalGames = 0;

    private List<float> aiWinRates = new List<float>();
    private List<int> aiScores = new List<int>();
    private List<int> playerScores = new List<int>();

    private string previousBounceTable = "None";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnBallHit()
    {
        bounceCount = 0;
    }

    public void OnBallBounce(string tableTag, bool isServe)
    {
        if (isPointScored || ball.IsResetting()) return;

        string lastHitter = ball.GetLastHitter();

        // ✅ AI가 공을 넘겨서 PlayerTable에 바운스한 경우 보상
        if (lastHitter == "AI" && tableTag == "PlayerTable")
        {
            aiPaddle.GetComponent<AIAgent>()?.AddReward(+0.3f); // 네트 넘기기 보상
        }

        // 🛑 자기 코트에 바운스 → 실점
        if (!isServe &&
            ((lastHitter == "Player" && tableTag == "PlayerTable") ||
             (lastHitter == "AI" && tableTag == "AITable")))
        {
            isPointScored = true;
            AwardPoint(Opponent(lastHitter), true, "Bounced on own court");
            return;
        }

        // 🛑 같은 테이블에 두 번 바운스 → 실점 (자신의 코트에서만 적용)
        if (!isServe && previousBounceTable == tableTag)
        {
            bool sameSide =
                (lastHitter == "AI" && tableTag == "AITable") ||
                (lastHitter == "Player" && tableTag == "PlayerTable");

            if (sameSide)
            {
                isPointScored = true;
                AwardPoint(Opponent(lastHitter), true, "Double bounce on own court");
                return;
            }
        }

        previousBounceTable = tableTag;
        bounceCount++;
    }

    public void OnBallOutOfBounds(string lastBounceTable)
    {
        if (isPointScored) return;
        isPointScored = true;

        string lastHitter = ball.GetLastHitter();
        if (lastHitter == "None")
        {
            lastHitter = ball.GetServeBy();
        }

        if ((lastHitter == "Player" && lastBounceTable == "PlayerTable") ||
            (lastHitter == "AI" && lastBounceTable == "AITable"))
        {
            AwardPoint(Opponent(lastHitter), true, "Out of bounds from own side");
        }
        else
        {
            AwardPoint(lastHitter, false, "Opponent failed to return");
        }
    }

    private void AwardPoint(string winner, bool fault, string reason)
    {
        if (winner == "Player")
        {
            playerScore++;
            Debug.Log(fault
                ? $"✅ Player scores (AI fault: {reason})"
                : $"✅ Player scores (Player win: {reason})");
        }
        else
        {
            aiScore++;
            Debug.Log(fault
                ? $"✅ AI scores (Player fault: {reason})"
                : $"✅ AI scores (AI win: {reason})");
        }

        Debug.Log($"🏓 Score: Player {playerScore} / AI {aiScore}");
        CheckGameEnd();
        Invoke(nameof(ResetAfterPoint), 0.1f);
    }

    private string Opponent(string player) => player == "Player" ? "AI" : "Player";

    private void CheckGameEnd()
    {
        if (playerScore >= pointsToWin || aiScore >= pointsToWin)
        {
            totalGames++;

            float winRate = (float)aiScore / (playerScore + aiScore);
            aiWinRates.Add(winRate);
            aiScores.Add(aiScore);
            playerScores.Add(playerScore);

            Debug.Log($"📊 [Set End] Game {totalGames}, AI Score: {aiScore}, Player Score: {playerScore}, Win Rate: {winRate:F2}");

            SaveWinRateData();

            playerScore = 0;
            aiScore = 0;
        }
    }

    private void SaveWinRateData()
    {
        string path = Path.Combine(Application.persistentDataPath, "AIWinRate.csv");
        bool fileExists = File.Exists(path);

        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            if (!fileExists)
                writer.WriteLine("Game,AI_Score,Player_Score,AI_WinRate");

            int index = aiWinRates.Count - 1;
            int ai = aiScores[index];
            int player = playerScores[index];
            float rate = aiWinRates[index];

            writer.WriteLine($"{totalGames},{ai},{player},{rate:F3}");
        }

        Debug.Log($"📁 WinRate appended at: {path}");
    }

    private void ResetAfterPoint()
    {
        string hitter = ball.GetLastHitter();
        string result = hitter == "Player" ? "PlayerScored" : "AIScored";

        logger?.LogPoint(totalGames, hitter, ball.GetPosition(), ball.GetVelocity().magnitude,
            aiPaddle.position.z, playerPaddle.position.z, result);

        bounceCount = 0;
        previousBounceTable = "None";
        isPointScored = false;

        ball.ResetBall(centerPoint.position);
    }

    public void ResetBounceCount()
    {
        bounceCount = 0;
        previousBounceTable = "None";
    }
}
