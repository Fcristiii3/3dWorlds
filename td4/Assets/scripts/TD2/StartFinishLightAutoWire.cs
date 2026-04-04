using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StartFinishLightAutoWire : MonoBehaviour
{
    [Header("Name Hints")]
    public string redLightNameHint = "Red";
    public string yellowLightNameHint = "Yellow";
    public string greenLightNameHint = "Green";

    [Header("Search Options")]
    public bool includeInactiveChildren = true;

    private bool hasAssignedLights;

    private void OnEnable()
    {
        TryAssignLightsToGameManager();
    }

    private void Start()
    {
        TryAssignLightsToGameManager();
    }

    public void TryAssignLightsToGameManager()
    {
        if (hasAssignedLights)
        {
            return;
        }

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        Light[] childLights = GetComponentsInChildren<Light>(includeInactiveChildren);
        if (childLights == null || childLights.Length == 0)
        {
            Debug.LogWarning($"StartFinishLightAutoWire on '{name}' could not find any child Light components.");
            return;
        }

        List<Light> remainingLights = new List<Light>(childLights);
        Light redLight = FindAndRemoveMatchingLight(remainingLights, redLightNameHint);
        Light yellowLight = FindAndRemoveMatchingLight(remainingLights, yellowLightNameHint);
        Light greenLight = FindAndRemoveMatchingLight(remainingLights, greenLightNameHint);

        if (redLight == null)
        {
            redLight = FindAndRemoveClosestColorLight(remainingLights, Color.red);
        }

        if (yellowLight == null)
        {
            yellowLight = FindAndRemoveClosestColorLight(remainingLights, Color.yellow);
        }

        if (greenLight == null)
        {
            greenLight = FindAndRemoveClosestColorLight(remainingLights, Color.green);
        }

        if (redLight == null && remainingLights.Count > 0)
        {
            redLight = RemoveFirstAvailableLight(remainingLights);
        }

        if (yellowLight == null && remainingLights.Count > 0)
        {
            yellowLight = RemoveFirstAvailableLight(remainingLights);
        }

        if (greenLight == null && remainingLights.Count > 0)
        {
            greenLight = RemoveFirstAvailableLight(remainingLights);
        }

        gameManager.racingLights = new[] { redLight, yellowLight, greenLight };

        for (int i = 0; i < gameManager.racingLights.Length; i++)
        {
            if (gameManager.racingLights[i] != null)
            {
                gameManager.racingLights[i].enabled = false;
            }
        }

        Debug.Log($"StartFinishLightAutoWire assigned lights: Red='{GetLightName(redLight)}', Yellow='{GetLightName(yellowLight)}', Green='{GetLightName(greenLight)}'.");

        hasAssignedLights = true;
    }

    private static Light FindAndRemoveMatchingLight(List<Light> remainingLights, string nameHint)
    {
        if (string.IsNullOrWhiteSpace(nameHint))
        {
            return null;
        }

        string loweredHint = nameHint.ToLowerInvariant();

        for (int i = 0; i < remainingLights.Count; i++)
        {
            Light candidate = remainingLights[i];
            if (candidate == null)
            {
                continue;
            }

            if (!candidate.name.ToLowerInvariant().Contains(loweredHint))
            {
                continue;
            }

            remainingLights.RemoveAt(i);
            return candidate;
        }

        return null;
    }

    private static Light FindAndRemoveClosestColorLight(List<Light> remainingLights, Color targetColor)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < remainingLights.Count; i++)
        {
            Light candidate = remainingLights[i];
            if (candidate == null)
            {
                continue;
            }

            float distance = GetColorDistance(candidate.color, targetColor);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            return null;
        }

        Light bestLight = remainingLights[bestIndex];
        remainingLights.RemoveAt(bestIndex);
        return bestLight;
    }

    private static float GetColorDistance(Color a, Color b)
    {
        float deltaR = a.r - b.r;
        float deltaG = a.g - b.g;
        float deltaB = a.b - b.b;
        return (deltaR * deltaR) + (deltaG * deltaG) + (deltaB * deltaB);
    }

    private static Light RemoveFirstAvailableLight(List<Light> remainingLights)
    {
        if (remainingLights.Count == 0)
        {
            return null;
        }

        Light light = remainingLights[0];
        remainingLights.RemoveAt(0);
        return light;
    }

    private static string GetLightName(Light light)
    {
        return light != null ? light.name : "None";
    }
}
