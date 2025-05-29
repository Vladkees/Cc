using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DroneInputManager : MonoBehaviour
{
    public TMP_InputField inputKronus;
    public TMP_InputField inputLyrion;
    public TMP_InputField inputMystara;
    public TMP_InputField inputEclipsia;
    public TMP_InputField inputFiora;

    public TextMeshProUGUI errorText;
    public TextMeshProUGUI dronesLeftText;
    public Button dronesSendButton;

    private int maxDrones = 999;

    public ServerConnector serverConnector;
    public GameManager gameManager;

    public void ValidateInputs()
    {
        int kronus = ParseInput(inputKronus.text);
        int lyrion = ParseInput(inputLyrion.text);
        int mystara = ParseInput(inputMystara.text);
        int eclipsia = ParseInput(inputEclipsia.text);
        int fiora = ParseInput(inputFiora.text);

        if (!(kronus >= lyrion && lyrion >= mystara && mystara >= eclipsia && eclipsia >= fiora))
        {
            errorText.text = "Значення повинні спадати: Kronus ≥ Lyrion ≥ Mystara ≥ Eclipsia ≥ Fiora";
            dronesLeftText.text = "";
            return;
        }

        int sum = kronus + lyrion + mystara + eclipsia + fiora;
        if (sum > maxDrones)
        {
            errorText.text = $"Ви ввели забагато дронів! Максимум {maxDrones}.";
            dronesLeftText.text = "";
            return;
        }

        errorText.text = "";
        dronesLeftText.text = $"Дронів залишилось: {maxDrones - sum}";

        int k = ParseInputSafe(inputKronus?.text);
        int l = ParseInputSafe(inputLyrion?.text);
        int m = ParseInputSafe(inputMystara?.text);
        int e = ParseInputSafe(inputEclipsia?.text);
        int f = ParseInputSafe(inputFiora?.text);

        int[] dronesCount = new int[5] { k, l, m, e, f };
        gameManager.SetDroneActivityLevels(dronesCount);

        if (maxDrones - sum == 0 && (kronus >= lyrion && lyrion >= mystara && mystara >= eclipsia && eclipsia >= fiora))
        {
            dronesSendButton.gameObject.SetActive(true);
        }
    }

    public void SendToServerIfValid()
    {
        int kronus = ParseInputSafe(inputKronus?.text);
        int lyrion = ParseInputSafe(inputLyrion?.text);
        int mystara = ParseInputSafe(inputMystara?.text);
        int eclipsia = ParseInputSafe(inputEclipsia?.text);
        int fiora = ParseInputSafe(inputFiora?.text);

        // Перевірка спадання
        if (!(kronus >= lyrion && lyrion >= mystara && mystara >= eclipsia && eclipsia >= fiora))
        {
            errorText.text = "Значення повинні спадати: Kronus ≥ Lyrion ≥ Mystara ≥ Eclipsia ≥ Fiora";
            dronesLeftText.text = "";
            return;
        }

        int sum = kronus + lyrion + mystara + eclipsia + fiora;
        if (sum > maxDrones)
        {
            errorText.text = $"Ви ввели забагато дронів! Максимум {maxDrones}.";
            dronesLeftText.text = "";
            return;
        }

        // Якщо все коректно — надсилаємо на сервер
        errorText.text = "";
        dronesLeftText.text = $"Дронів залишилось: {maxDrones - sum}";

        // Тут викликаємо метод серверного з'єднання — наприклад:
        dronesSendButton.gameObject.SetActive(false);
        serverConnector.SendDroneDistribution(serverConnector.ID, kronus, lyrion, mystara, eclipsia, fiora);
        gameManager.SetState(GameState.ShowResults);
    }

    public void ClearMessages()
    {
        errorText.text = "";
        dronesLeftText.text = "";
        inputKronus.textComponent.enabled = false;
        inputLyrion.textComponent.enabled = false;
        inputMystara.textComponent.enabled = false;
        inputEclipsia.textComponent.enabled = false;
        inputFiora.textComponent.enabled = false;
    }

    private int ParseInput(string text)
    {
        if (int.TryParse(text, out int val))
            return val;
        return 0;
    }

    // Метод для безпечного парсингу
    int ParseInputSafe(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        if (int.TryParse(input, out int value))
            return value;

        return 0; // якщо в тексті не число
    }
}