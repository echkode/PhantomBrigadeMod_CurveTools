// From Artyom on Discord
// https://discord.com/channels/380929397445754890/778886378691756053/932085210353401868

using UnityEngine;

public class CurveEditor : MonoBehaviour
{
    public AnimationCurve curve;
    private AnimationCurveSerialized curveSerialized;

    [ContextMenu("Print Curve")]
    void PrintCurve()
    {
        curveSerialized = curve.ToSerializedFormat();
        var sb = new System.Text.StringBuilder();

        for (var i = 0; i < curveSerialized.keys.Length; ++i)
        {
            var key = curveSerialized.keys[i];
            sb.Append(System.Environment.NewLine)
                .AppendFormat("{0}:", i)
                .Append(System.Environment.NewLine)
                .AppendFormat("- tv_tg: {0}", key.timeValueTangents)
                .Append(System.Environment.NewLine)
                .AppendFormat("- w: {0}", key.weights)
                .Append(System.Environment.NewLine)
                .AppendFormat("- m: {0}", key.modes);
        }

        Debug.Log(sb.ToString());
    }
}
