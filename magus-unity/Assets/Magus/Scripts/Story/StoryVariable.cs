using System;

namespace magus.story
{
    [Serializable]
    public class StoryVariable
    {
        public enum ValueType
        {
            Bool,
            Int,
            Float,
            String
        }

        public ValueType Type;
        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public string StringValue;

        public static StoryVariable FromBool(bool value) => new StoryVariable { Type = ValueType.Bool, BoolValue = value };
        public static StoryVariable FromInt(int value) => new StoryVariable { Type = ValueType.Int, IntValue = value };
        public static StoryVariable FromFloat(float value) => new StoryVariable { Type = ValueType.Float, FloatValue = value };
        public static StoryVariable FromString(string value) => new StoryVariable { Type = ValueType.String, StringValue = value };
    }
}
