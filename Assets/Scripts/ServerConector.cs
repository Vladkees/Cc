using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ServerConnector : MonoBehaviour
{
    private string register = "https://0eb8-93-170-117-28.ngrok-free.app/game_server/register.php";
    private string lobby = "https://0eb8-93-170-117-28.ngrok-free.app/game_server/start_game.php";
    private string move = "https://0eb8-93-170-117-28.ngrok-free.app/game_server/submit_move.php";
    private string results = "https://0eb8-93-170-117-28.ngrok-free.app/game_server/get_results.php";

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
        string jsonData = JsonConvert.SerializeObject(new PlayerData { username = username });
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

            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseText);

                if (!string.IsNullOrEmpty(errorResponse.error))
                {
                    Debug.LogError("Помилка від сервера: " + errorResponse.error);
                    onComplete?.Invoke(false);
                }
                else
                {
                    // Шукаємо player_id у відповіді, припускаємо, що він є
                    var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);
                    if (jsonObj != null && jsonObj.ContainsKey("player_id"))
                    {
                        int playerId = Convert.ToInt32(jsonObj["player_id"]);
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
            catch (Exception ex)
            {
                Debug.LogError("Помилка парсингу JSON: " + ex.Message);
                onComplete?.Invoke(false);
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

                try
                {
                    var response = JsonConvert.DeserializeObject<GameStatusResponse>(json);

                    if (response != null)
                    {
                        Debug.Log("Status: " + response.status);

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
                catch (Exception ex)
                {
                    Debug.LogError("Помилка парсингу JSON: " + ex.Message);
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

                    try
                    {
                        var response = JsonConvert.DeserializeObject<GameStatusResponse>(json);

                        if (response != null)
                        {
                            if (!string.IsNullOrEmpty(response.message))
                            {
                                gameManager.messageText.text = response.message;
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
                    catch (Exception ex)
                    {
                        Debug.LogError("Помилка парсингу JSON: " + ex.Message);
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

        string json = JsonConvert.SerializeObject(data);
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
            using (UnityWebRequest request = UnityWebRequest.Get(results))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Помилка запиту: " + request.error);
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                string json = request.downloadHandler.text;

                Debug.Log("Отриманий JSON: " + json);

                try
                {
                    RoundResponse response = JsonConvert.DeserializeObject<RoundResponse>(json);

                    if (response == null)
                    {
                        Debug.LogError("Не вдалося розпарсити RoundResponse");
                        yield return new WaitForSeconds(1f);
                        continue;
                    }

                    success = response.success;

                    if (!success)
                    {
                        Debug.Log("Очікуємо завершення раунду... Спроба ще через 1 секунду");
                        yield return new WaitForSeconds(1f);
                        continue;
                    }

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
                catch (Exception ex)
                {
                    Debug.LogError("Помилка парсингу JSON: " + ex.Message);
                    yield return new WaitForSeconds(1f);
                }
            }
        }
    }

    // Класи для десеріалізації

    [Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    [Serializable]
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

    [Serializable]
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
