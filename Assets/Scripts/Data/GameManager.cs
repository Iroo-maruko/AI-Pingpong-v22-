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

        if (!isServe &&
            ((lastHitter == "Player" && tableTag == "PlayerTable") ||
             (lastHitter == "AI" && tableTag == "AITable")))
        {
            isPointScored = true;
            AwardPoint(Opponent(lastHitter), true);
            return;
        }

        if (!isServe && previousBounceTable == tableTag)
        {
            isPointScored = true;
            AwardPoint(Opponent(lastHitter), true);
            return;
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
            lastHitter = ball.GetServeBy();

        if ((lastHitter == "Player" && lastBounceTable == "PlayerTable") ||
            (lastHitter == "AI" && lastBounceTable == "AITable"))
        {
            AwardPoint(Opponent(lastHitter), true);
        }
        else
        {
            AwardPoint(lastHitter, false);
        }
    }

    private void AwardPoint(string winner, bool fault)
    {
        if (winner == "Player")
        {
            playerScore++;
            aiPaddle.GetComponent<AIAgent>()?.MissedBallPenalty();
        }
        else
        {
            aiScore++;
            aiPaddle.GetComponent<AIAgent>()?.SuccessfulPoint();
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
            SaveWinRateData();
            playerScore = 0; aiScore = 0;
        }
    }

    public void ResetAfterPoint()
    {
        bounceCount = 0;
        isPointScored = false;
        previousBounceTable = "None";

        string hitter = ball.GetLastHitter();
        string result = hitter == "Player" ? "PlayerScored" : "AIScored";

        logger.LogPoint(totalGames, hitter, ball.GetPosition(), ball.GetVelocity().magnitude,
            aiPaddle.position.z, playerPaddle.position.z, result);

        ball.ResetBall(centerPoint.position);
    }

    private void SaveWinRateData()
    {
        string path = Path.Combine(Application.dataPath, "AIWinRate.csv");
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Game,AIWinRate");
            for (int i = 0; i < aiWinRates.Count; i++)
                writer.WriteLine($"{i + 1},{aiWinRates[i]}");
        }
    }

    public void ResetBounceCount()
    {
        bounceCount = 0;
        previousBounceTable = "None";
    }
}
