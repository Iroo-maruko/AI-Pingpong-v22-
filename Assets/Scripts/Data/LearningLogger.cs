using UnityEngine;
using System.IO;

public class LearningLogger : MonoBehaviour
{
    private string logPath;

    private void Awake()
    {
        logPath = Path.Combine(Application.dataPath, "LearningData.csv");

        // 첫 번째 라인에 헤더 작성
        if (!File.Exists(logPath))
        {
            using (StreamWriter writer = new StreamWriter(logPath))
            {
                writer.WriteLine("Game,LastHitter,BallX,BallY,BallZ,BallSpeed,AIPaddleZ,PlayerPaddleZ,Result");
            }
        }
    }

    public void LogPoint(int game, string lastHitter, Vector3 ballPos, float ballSpeed, float aiZ, float playerZ, string result)
    {
        using (StreamWriter writer = new StreamWriter(logPath, true))
        {
            writer.WriteLine($"{game},{lastHitter},{ballPos.x:F2},{ballPos.y:F2},{ballPos.z:F2},{ballSpeed:F2},{aiZ:F2},{playerZ:F2},{result}");
        }
        Debug.Log($"📝 학습 로그 기록됨: {result}");
    }
}
