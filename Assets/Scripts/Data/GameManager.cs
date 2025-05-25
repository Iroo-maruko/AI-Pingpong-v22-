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
    private string previousBounceTable = "None";
    private int totalGames = 0;
    private List<float> aiWinRates = new List<float>();
    private bool isPointScored = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnBallHit()
    {
        bounceCount = 0;
    }

    public void OnBallOutOfBounds(string serveBy, string lastBounceTable)
    {
        if (isPointScored) return;
        isPointScored = true;

        string lastHitter = ball.GetLastHitter();

        bool aiFault = (lastHitter == "AI" && lastBounceTable == "AITable");
        bool playerFault = (lastHitter == "Player" && lastBounceTable == "PlayerTable");

        if (aiFault)
        {
            playerScore++;
            Debug.Log("❌ AI hit but landed on own table → Player scores");
            aiPaddle.GetComponent<AIAgent>()?.MissedBallPenalty();
        }
        else if (playerFault)
        {
            aiScore++;
            Debug.Log("❌ Player hit but landed on own table → AI scores");
            aiPaddle.GetComponent<AIAgent>()?.SuccessfulPoint();
        }
        else
        {
            // fallback
            if (serveBy == "Player")
            {
                aiScore++;
                Debug.Log("⚠️ Out-of-bounds fallback → Player served, AI scores");
            }
            else
            {
                playerScore++;
                Debug.Log("⚠️ Out-of-bounds fallback → AI served, Player scores");
            }

            if (lastHitter == "Player")
                aiPaddle.GetComponent<AIAgent>()?.SuccessfulPoint();
            else
                aiPaddle.GetComponent<AIAgent>()?.MissedBallPenalty();
        }

        Debug.Log($"❌ Ball out → Score: Player {playerScore} / AI {aiScore}");
        CheckGameEnd();
        Invoke(nameof(ResetAfterPoint), 0.1f);
    }

    public void OnBallHitNet()
    {
        if (isPointScored || ball.IsResetting()) return;

        isPointScored = true;
        string lastHitter = ball.GetLastHitter();

        if (lastHitter == "Player") aiScore++;
        else playerScore++;

        Debug.Log($"🪢 Net hit → Score: Player {playerScore} / AI {aiScore}");
        CheckGameEnd();
        Invoke(nameof(ResetAfterPoint), 0.1f);
    }

    public void OnBallBounce(bool isServe = false)
    {
        if (isPointScored || ball.IsResetting()) return;

        string currentBounceTable = ball.GetLastBounceTable();

        if (isServe)
        {
            previousBounceTable = currentBounceTable;
            Debug.Log("➖ First bounce during serve → No score");
            return;
        }

        bounceCount++;

        if (bounceCount == 1)
        {
            previousBounceTable = currentBounceTable;
            return;
        }

        if (bounceCount >= 2)
        {
            isPointScored = true;
            string lastHitter = ball.GetLastHitter();

            bool isSameSideMiss =
                (lastHitter == "Player" && currentBounceTable == "PlayerTable") ||
                (lastHitter == "AI" && currentBounceTable == "AITable");

            if (isSameSideMiss)
            {
                if (lastHitter == "Player")
                {
                    aiScore++;
                    Debug.Log("❌ Player missed → AI scores");
                    aiPaddle.GetComponent<AIAgent>()?.SuccessfulPoint();
                }
                else
                {
                    playerScore++;
                    Debug.Log("❌ AI missed → Player scores");
                    aiPaddle.GetComponent<AIAgent>()?.MissedBallPenalty();
                }
            }
            else
            {
                if (lastHitter == "Player")
                {
                    playerScore++;
                    Debug.Log("🎯 Point scored by Player");
                    aiPaddle.GetComponent<AIAgent>()?.MissedBallPenalty();
                }
                else
                {
                    aiScore++;
                    Debug.Log("🎯 Point scored by AI");
                    aiPaddle.GetComponent<AIAgent>()?.SuccessfulPoint();
                }
            }

            Debug.Log($"🎯 Point scored: Player {playerScore} / AI {aiScore}");
            CheckGameEnd();
            Invoke(nameof(ResetAfterPoint), 0.1f);
        }
    }

    private void CheckGameEnd()
    {
        if (playerScore >= pointsToWin || aiScore >= pointsToWin)
        {
            totalGames++;
            float winRate = (float)aiScore / (playerScore + aiScore);
            aiWinRates.Add(winRate);
            SaveWinRateData();

            playerScore = 0;
            aiScore = 0;
        }
    }

    public void ResetAfterPoint()
    {
        bounceCount = 0;
        isPointScored = false;
        previousBounceTable = "None";

        Vector3 ballPos = ball.GetPosition();
        float ballSpeed = ball.GetVelocity().magnitude;
        string lastHitter = ball.GetLastHitter();
        string result = lastHitter == "Player" ? "PlayerScored" : "AIScored";

        logger.LogPoint(totalGames, lastHitter, ballPos, ballSpeed, aiPaddle.position.z, playerPaddle.position.z, result);
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
        Debug.Log("🔄 Bounce count reset (new hitter)");
    }
}
