using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerConnector : MonoBehaviour
{
    private string register = "https://0f2d-93-170-117-28.ngrok-free.app/game_server/register.php";
    private string lobby = "https://0f2d-93-170-117-28.ngrok-free.app/game_server/start_game.php";
    private string move = "https://0f2d-93-170-117-28.ngrok-free.app/game_server/submit_move.php";
    private string results = "https://0f2d-93-170-117-28.ngrok-free.app/game_server/get_results.php";

    public int ID;
    public Dictionary<string, int> playerScores = new Dictionary<string, int>();
    public GameManager gameManager;
    public ScoreDisplay scoreDisplay;

    public void RegisterPlayer(string username, Action<bool> onComplete)
    {
        StartCoroutine(RegisterCoroutine(username, onComplete));
    }

    private IEnumerator RegisterCoroutine(string username, Action<bool> onComplete)
    {
        string jsonData = JsonUtility.ToJson(new PlayerData { username = username });
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(register, "POST");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Відповідь сервера: " + responseText);

            // Парсимо поле error
            ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);

            if (!string.IsNullOrEmpty(errorResponse.error))
            {
                Debug.LogError("Помилка від сервера: " + errorResponse.error);
                onComplete?.Invoke(false);
            }
            else
            {
                // 🔍 Знаходимо player_id за допомогою регулярного виразу
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(responseText, "\"player_id\"\\s*:\\s*(\\d+)");
                if (match.Success)
                {
                    int playerId = int.Parse(match.Groups[1].Value);
                    PlayerPrefs.SetInt("player_id", playerId);
                    PlayerPrefs.Save();

                    ID = playerId;
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError("player_id не знайдено у відповіді.");
                    onComplete?.Invoke(false);
                }
            }
        }
        else
        {
            Debug.LogError("Помилка при підключенні до сервера: " + request.error);
            onComplete?.Invoke(false);
        }
    }


    public void CheckStatus()
    {
        StartCoroutine(GetStatusCoroutine());
    }

    private IEnumerator GetStatusCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(lobby))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Відповідь сервера: " + json);

                // Розбираємо JSON
                GameStatusResponse response = JsonUtility.FromJson<GameStatusResponse>(json);

                if (response != null)
                {
                    Debug.Log("Status: " + response.status);

                    // Тут можна щось робити з цим статусом, наприклад:
                    if (response.status == "waiting")
                    {
                        Debug.Log("Гра ще не почалася");
                    }
                    else if (response.status == "started")
                    {
                        Debug.Log("Гра почалася");
                    }
                }
                else
                {
                    Debug.LogError("Не вдалося розпарсити JSON");
                }
            }
            else
            {
                Debug.LogError("Помилка запиту: " + request.error);
            }
        }
    }

    public void StartCheckingGameStatus(Action onGameStarted, Action<int> onTimeLeftUpdate)
    {
        StartCoroutine(CheckGameStatusLoop(onGameStarted, onTimeLeftUpdate));
    }

    private IEnumerator CheckGameStatusLoop(Action onGameStarted, Action<int> onTimeLeftUpdate)
    {
        while (true)
        {
            string urlWithTimestamp = lobby + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (UnityWebRequest www = UnityWebRequest.Get(urlWithTimestamp))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("HTTP-помилка: " + www.error);
                }
                else
                {
                    string json = www.downloadHandler.text;
                    Debug.Log("CheckGameStatusLoop JSON: " + json);

                    GameStatusResponse response = null;
                    bool parseSuccess = false;

                    try
                    {
                        response = JsonUtility.FromJson<GameStatusResponse>(json);
                        parseSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Помилка парсингу JSON: " + ex.Message);
                    }

                    if (!string.IsNullOrEmpty(response.message))
                    {
                        gameManager.messageText.text = response.message;
                    }

                    if (!parseSuccess)
                    {
                        yield return new WaitForSeconds(1f);
                        continue;
                    }

                    if (response.status == "waiting")
                    {
                        if (response.time_left.HasValue)
                            onTimeLeftUpdate?.Invoke(response.time_left.Value);
                    }
                    else if (response.status == "started")
                    {
                        Debug.Log("Гра почалася!");
                        onGameStarted?.Invoke();
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning("Невідомий статус гри: " + response.status);
                    }
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void SendDroneDistribution(int playerId, int kronus, int lyrion, int mystara, int eclipsia, int fiora)
    {
        DroneDistributionData data = new DroneDistributionData
        {
            player_id = playerId,
            kronus = kronus,
            lyrion = lyrion,
            mystara = mystara,
            eclipsia = eclipsia,
            fiora = fiora
        };

        string json = JsonUtility.ToJson(data);
        Debug.Log("JSON що надсилається: " + json);
        StartCoroutine(PostJsonRequest(move, json));
    }

    private IEnumerator PostJsonRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Успішно надіслано: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Помилка запиту: " + request.error);
        }
    }

    public void GetResults()
    {
        StartCoroutine(CheckUntilSuccess());
    }

    IEnumerator CheckUntilSuccess()
    {
        bool success = false;

        while (!success)
        {
            UnityWebRequest request = UnityWebRequest.Get(results);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Помилка запиту: " + request.error);
                yield return new WaitForSeconds(1f);
                continue;
            }

            string json = request.downloadHandler.text;

            // Спроба розпарсити, навіть якщо success == false
            Debug.Log("Отриманий JSON: " + json);
            RoundResponseWrapper wrapper = JsonUtility.FromJson<RoundResponseWrapper>("{\"wrapper\":" + json + "}");
            RoundResponse response = wrapper.wrapper;

            success = response.success;

            if (!success)
            {
                Debug.Log("Очікуємо завершення раунду... Спроба ще через 1 секунду");
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Якщо success == true: обробити результати
            Debug.Log("Раунд завершено. Отримуємо результати...");

            playerScores.Clear();
            foreach (ResultEntry entry in response.results)
            {
                playerScores[entry.username] = entry.total_score;
            }

            foreach (var kvp in playerScores)
            {
                Debug.Log($"Гравець {kvp.Key} має рахунок {kvp.Value}");
            }

            scoreDisplay.UpdateScoreText(playerScores);
            if (response.round > 4)
            {
                gameManager.SetState(GameState.GameOver);
                Vector3 pos = scoreDisplay.transform.position;
                pos.x = 0f;
                scoreDisplay.transform.position = pos;
            }
            else
            {
                gameManager.SetState(GameState.RoundInProgress);
            }
        }
    }

    [Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string username;
    }

    [Serializable]
    public class GameStatusResponse
    {
        public string status;
        public int? time_left; // nullable, бо іноді його немає
        public string message;
    }

    [System.Serializable]
    public class DroneData
    {
        public int Kronus;
        public int Lyrion;
        public int Mystara;
        public int Eclipsia;
        public int Fiora;
    }

    [System.Serializable]
    public class DroneDistributionData
    {
        public int player_id;
        public int kronus;
        public int lyrion;
        public int mystara;
        public int eclipsia;
        public int fiora;
    }

    [Serializable]
    public class RoundResponseWrapper
    {
        public RoundResponse wrapper;
    }

    [Serializable]
    public class RoundResponse
    {
        public bool success;
        public bool round_completed;
        public int round;
        public ResultEntry[] results;
        public bool is_new_round;
        public int? new_round;
        public long timestamp;
    }

    [Serializable]
    public class ResultEntry
    {
        public int player_id;
        public string username;
        public int kronus;
        public int lyrion;
        public int mystara;
        public int eclipsia;
        public int fiora;
        public int round_score;
        public int total_score;
    }
}