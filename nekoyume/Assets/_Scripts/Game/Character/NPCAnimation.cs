using System;
using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public static class NPCAnimation
    {
        public enum Type
        {
            Appear,
            Appear_02,
            Appear_03,
            Greeting,
            Greeting_02,
            Greeting_03,
            Open,
            Open_02,
            Open_03,
            Idle,
            Idle_02,
            Idle_03,
            Emotion,
            Emotion_02,
            Emotion_03,
            Emotion_04,
            Emotion_05,
            Touch,
            Touch_02,
            Touch_03,
            Loop,
            Loop_02,
            Loop_03,
            Loop_Aura,
            Loop_Rune,
            Disappear,
            Disappear_02,
            Disappear_03,
            Over,
            Click,
            Appear_01,
            Disappear_01,
            Motion_01,
        }

        public static readonly List<Type> List = new();

        static NPCAnimation()
        {
            var values = Enum.GetValues(typeof(Type));
            foreach (var value in values)
            {
                List.Add((Type)value);
            }
        }
    }

    public class InvalidNPCAnimationTypeException : Exception
    {
        public InvalidNPCAnimationTypeException(string message) : base(message)
        {
        }
    }
}
