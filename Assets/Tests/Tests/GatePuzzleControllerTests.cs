using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class GatePuzzleControllerTests
{
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found.");
        field.SetValue(target, value);
    }

    private static void CallPrivateMethod(object target, string methodName)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(m, $"Method '{methodName}' not found.");
        m.Invoke(target, null);
    }

    private static TextMeshProUGUI CreateTmpText(Transform parent)
    {
        var go = new GameObject("RuneText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TextMeshProUGUI>();
    }

    [UnityTest]
    public IEnumerator Confirm_WhenPatternCorrect_DeactivatesPuzzle()
    {
        var puzzleGo = new GameObject("GatePuzzle");
        var controller = puzzleGo.AddComponent<GatePuzzleController>();

        var rune0 = CreateTmpText(puzzleGo.transform);

        SetPrivateField(controller, "runeTexts", new[] { rune0 });
        SetPrivateField(controller, "symbolOptions", new[] { "0", "1" });
        SetPrivateField(controller, "correctPattern", new[] { 0 });

        SetPrivateField(controller, "wrongPatternDialogue", null);
        SetPrivateField(controller, "puzzleCompleteDialogue", null);
        SetPrivateField(controller, "gateController", null);

        CallPrivateMethod(controller, "Awake");
        CallPrivateMethod(controller, "OnEnable");

        yield return null;

        controller.Confirm();
        yield return null;

        Assert.IsFalse(puzzleGo.activeSelf, "Puzzle should deactivate after correct confirm when gateController is null.");
    }

    [UnityTest]
    public IEnumerator Confirm_WhenPatternWrong_DoesNotDeactivatePuzzle()
    {
        var puzzleGo = new GameObject("GatePuzzle");
        var controller = puzzleGo.AddComponent<GatePuzzleController>();

        var rune0 = CreateTmpText(puzzleGo.transform);

        SetPrivateField(controller, "runeTexts", new[] { rune0 });
        SetPrivateField(controller, "symbolOptions", new[] { "0", "1" });
        SetPrivateField(controller, "correctPattern", new[] { 0 });

        SetPrivateField(controller, "wrongPatternDialogue", null);
        SetPrivateField(controller, "puzzleCompleteDialogue", null);
        SetPrivateField(controller, "gateController", null);

        CallPrivateMethod(controller, "Awake");
        CallPrivateMethod(controller, "OnEnable");

        yield return null;

        controller.CycleRune(0);
        controller.Confirm();
        yield return null;

        Assert.IsTrue(puzzleGo.activeSelf, "Puzzle should remain active after incorrect confirm.");
    }
}