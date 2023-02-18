// From Artyom on Discord
using System;
using UnityEngine;
using YamlDotNet.Serialization;

[Serializable]
public class AnimationCurveSerialized
{
    public WrapMode modePostWrap;
    public WrapMode modePreWrap;
    public KeyframeSerialized[] keys;

    public static explicit operator AnimationCurveSerialized(AnimationCurve source) => source.ToSerializedFormat();
    public static explicit operator AnimationCurve(AnimationCurveSerialized source) => source.ToRuntimeFormat();
}

[Serializable]
public struct KeyframeSerialized
{
	[YamlMember(Alias = "tv_tg")]
	public Vector4 timeValueTangents;
	[YamlMember(Alias = "w")]
	public Vector2 weights;
	[YamlMember(Alias = "m")]
    public Vector2Int modes;

    public KeyframeSerialized(Keyframe source)
    {
        timeValueTangents = new Vector4(source.time, source.value, source.inTangent, source.outTangent);
        weights = new Vector2(source.inWeight, source.outWeight);
        modes = new Vector2Int(source.tangentMode, (int)source.weightedMode);
    }
}

public static class AnimationCurveSerializedUtility
{
    public static AnimationCurve ToRuntimeFormat(this AnimationCurveSerialized source)
    {
        if (source == null)
        {
            Debug.LogWarning("Failed to deserialize animation curve due to serialized representation being null");
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        }

        var keys = new Keyframe[source.keys.Length];
        for (int i = 0; i < source.keys.Length; ++i)
        {
            var keyframe = source.keys[i];

            float time = keyframe.timeValueTangents.x; // keyframe.time;
            float value = keyframe.timeValueTangents.y; // keyframe.value;
            float inTangent = keyframe.timeValueTangents.z; // keyframe.inTangent;
            float outTangent = keyframe.timeValueTangents.w; // keyframe.outTangent;
            float inWeight = keyframe.weights.x; // keyframe.inWeight;
            float outWeight = keyframe.weights.y; // keyframe.outWeight;

			keys[i] = new Keyframe(
				time,
				value,
				inTangent,
				outTangent,
				inWeight,
				outWeight)
			{
				tangentMode = keyframe.modes.x, // keyframe.tangentMode;
				weightedMode = (WeightedMode)keyframe.modes.y // (WeightedMode)keyframe.weightedMode;
			};
		}

		var result = new AnimationCurve(keys)
		{
			postWrapMode = source.modePostWrap,
			preWrapMode = source.modePreWrap
		};

		return result;
    }

    public static AnimationCurveSerialized ToSerializedFormat(this AnimationCurve source)
    {
        if (source == null)
        {
            Debug.LogWarning("Failed to create serialized animation curve due to source being null!");
            return null;
        }

		var result = new AnimationCurveSerialized
		{
			modePostWrap = source.postWrapMode,
			modePreWrap = source.preWrapMode,
			keys = new KeyframeSerialized[source.keys.Length]
		};

        for (int i = 0; i < source.keys.Length; i += 1)
        {
            result.keys[i] = new KeyframeSerialized(source.keys[i]);
        }
        return result;
    }
}