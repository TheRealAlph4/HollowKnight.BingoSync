﻿using UnityEngine;

namespace BingoSync
{
    public enum Colors
    {
        Orange,
        Red,
        Blue,
        Green,
        Purple,
        Navy,
        Teal,
        Brown,
        Pink,
        Yellow,
        Blank,
    }

    public static class ColorExtensions
    {
        public static Color GetColor(this Colors color)
        {
            return color switch
            {
                Colors.Orange => new(1.000f, 0.612f, 0.070f),
                Colors.Red => new(1.000f, 0.286f, 0.267f),
                Colors.Blue => new(0.251f, 0.612f, 1.000f),
                Colors.Green => new(0.192f, 0.847f, 0.078f),
                Colors.Purple => new(0.510f, 0.176f, 0.749f),
                Colors.Navy => new(0.051f, 0.282f, 0.710f),
                Colors.Teal => new(0.255f, 0.588f, 0.584f),
                Colors.Brown => new(0.671f, 0.361f, 0.137f),
                Colors.Pink => new(0.929f, 0.525f, 0.667f),
                Colors.Yellow => new(0.847f, 0.816f, 0.078f),
                _ => new(0f, 0f, 0f),
            };
        }

        public static string GetName(this Colors color)
        {
            return color switch
            {
                Colors.Orange => "orange",
                Colors.Red => "red",
                Colors.Blue => "blue",
                Colors.Green => "green",
                Colors.Purple => "purple",
                Colors.Navy => "navy",
                Colors.Teal => "teal",
                Colors.Brown => "brown",
                Colors.Pink => "pink",
                Colors.Yellow => "yellow",
                _ => "blank",
            };
        }

        public static Colors FromName(string color)
        {
            return color switch
            {
                "orange" => Colors.Orange,
                "red" => Colors.Red,
                "blue" => Colors.Blue,
                "green" => Colors.Green,
                "purple" => Colors.Purple,
                "navy" => Colors.Navy,
                "teal" => Colors.Teal,
                "brown" => Colors.Brown,
                "pink" => Colors.Pink,
                "yellow" => Colors.Yellow,
                _ => Colors.Blank,
            };
        }
    }
}