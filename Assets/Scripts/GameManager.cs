using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using System;
using System.Collections; 
public enum GameState
{
    EnterName,
    WaitingForOthers,
    RoundInProgress,
    ShowResults,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public GameState currentState = GameState.EnterName;
    public ServerConnector serverConnector;
    public int timeLeft;
    public TMP_InputField[] droneInputs;
    public DroneInputManager validator;

    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button submitButton;
    public TextMeshProUGUI messageText;

    [Header("GameObjects References")]
    public Light2D Light;
    public DroneOrbitController[] droneControllers;

    private string playerName;
 
  private void Start()
{
    submitButton.onClick.AddListener(OnSubmitName);
    SetState(GameState.EnterName);

    

    foreach (var input in droneInputs)
    {
        input.onValueChanged.AddListener(_ =>
        {
            if (currentState == GameState.RoundInProgress)
                validator.ValidateInputs();
        });
    }
}

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.EnterName:
                messageText.text = "Enter Your Nickname:";
                nameInputField.gameObject.SetActive(true);
                submitButton.gameObject.SetActive(true);
                Light.enabled = false;
                break;

            case GameState.WaitingForOthers:
                messageText.text = $"Waiting for other players";
                Light.enabled = true;
                nameInputField.gameObject.SetActive(false);
                submitButton.gameObject.SetActive(false);
                serverConnector.StartCheckingGameStatus(OnGameStarted, SetTimeLeft);
                break;

            case GameState.RoundInProgress:
                messageText.text = $"Distribute drones across the planets";
                DeactivateAllDronesInAllControllers();
                Light.enabled = true;
                break;

            case GameState.ShowResults:
                messageText.text = $"Waiting for other players";
                serverConnector.GetResults();
                Light.enabled = true;
                break;

            case GameState.GameOver:
                messageText.text = $"Game over";
                Light.enabled = true;
                DeactivateAllDronesInAllControllers();
                break;
        }

        SetGameState(currentState);
    }

    private void OnSubmitName()
    {
        string input = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            messageText.text = "Нікнейм не може бути порожнім!";
            return;
        }

        playerName = input;

        serverConnector.RegisterPlayer(playerName, success =>
        {
            if (success)
            {
                Debug.Log("Гравця зареєстровано!");
                SetState(GameState.WaitingForOthers);
            }
            else
            {
                Debug.Log("Не вдалося зареєструватися.");
            }
        });
    }

    public void SetGameState(GameState newState)
    {
        bool interactable = newState == GameState.RoundInProgress;

        foreach (var input in droneInputs)
        {
            input.interactable = interactable;
        }

        if (!interactable)
            validator.ClearMessages();
        else
        {
            validator.inputKronus.textComponent.enabled = true;
            validator.inputLyrion.textComponent.enabled = true;
            validator.inputMystara.textComponent.enabled = true;
            validator.inputEclipsia.textComponent.enabled = true;
            validator.inputFiora.textComponent.enabled = true;
            validator.ValidateInputs();
        }
    }

    private void SetTimeLeft(int seconds)
    {
        timeLeft = seconds;
        Debug.Log("Залишилось часу: " + timeLeft);
    }

    private void OnGameStarted()
    {
        SetState(GameState.RoundInProgress);
    }

    public void DeactivateAllDronesInAllControllers()
    {
        foreach (var controller in droneControllers)
        {
            controller.DeactivateAllDrones();
        }
    }
    
    public void SetDroneActivityLevels(int[] values)
    {
        if (values.Length != 5 || droneControllers.Length < 5)
        {
            Debug.LogWarning("Очікується масив з 5 значень і щонайменше 5 контролерів.");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            int rawValue = values[i];
            int mappedValue = Mathf.Clamp(rawValue / 100 + 1, 0, 10); // 0–99 => 1, ..., 900–999 => 10

            // Спеціальний випадок: якщо rawValue == 0, mappedValue має бути 0
            if (rawValue == 0)
                mappedValue = 0;

            droneControllers[i].ActivateDrones(mappedValue);
        }
    }
}