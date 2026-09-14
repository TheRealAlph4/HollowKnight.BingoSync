using System.Collections.Generic;
using UnityEngine;

namespace BingoSync.Clients.ColorManagement
{
    public class BingoSyncColorManager : IColorManager
    {
        public List<int> Colors => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        public Color ColorOf(int color)
        {
            if (Controller.GlobalSettings.ColorScheme == 1)
            {
                return color switch
                {
                    0 => new(1.000f, 0.500f, 0.000f), // Orange
                    1 => new(1.000f, 0.000f, 0.000f), // Red
                    2 => new(0.000f, 0.500f, 1.000f), // Blue
                    3 => new(0.000f, 1.000f, 0.000f), // Green
                    4 => new(0.500f, 0.250f, 0.750f), // Purple
                    5 => new(0.000f, 0.000f, 0.750f), // Navy
                    6 => new(0.000f, 0.750f, 0.750f), // Teal
                    7 => new(0.750f, 0.375f, 0.125f), // Brown
                    8 => new(1.000f, 0.500f, 0.750f), // Pink
                    9 => new(1.000f, 1.000f, 0.000f), // Yellow
                    _ => new(0f, 0f, 0f),
                };
            }
            if (Controller.GlobalSettings.ColorScheme == 2)
            {
                return color switch
                {
                    0 => new(1.000f, 0.500f, 0.000f), // Orange
                    1 => new(1.000f, 0.000f, 0.000f), // Red
                    2 => new(0.500f, 1.000f, 1.000f), // Blue
                    3 => new(0.000f, 1.000f, 0.000f), // Green
                    4 => new(0.500f, 0.375f, 1.000f), // Purple
                    5 => new(0.000f, 0.180f, 1.000f), // Navy
                    6 => new(0.950f, 0.950f, 0.950f), // Teal
                    7 => new(0.575f, 0.375f, 0.080f), // Brown
                    8 => new(1.000f, 0.000f, 1.000f), // Pink
                    9 => new(1.000f, 1.000f, 0.000f), // Yellow
                    _ => new(0f, 0f, 0f),
                };
            }
            return color switch
            {
                0 => new(1.000f, 0.612f, 0.070f), // Orange
                1 => new(1.000f, 0.286f, 0.267f), // Red
                2 => new(0.251f, 0.612f, 1.000f), // Blue
                3 => new(0.192f, 0.847f, 0.078f), // Green
                4 => new(0.510f, 0.176f, 0.749f), // Purple
                5 => new(0.051f, 0.282f, 0.710f), // Navy
                6 => new(0.255f, 0.588f, 0.584f), // Teal
                7 => new(0.671f, 0.361f, 0.137f), // Brown
                8 => new(0.929f, 0.525f, 0.667f), // Pink
                9 => new(0.847f, 0.816f, 0.078f), // Yellow
                _ => new(0f, 0f, 0f),
            };
        }

        public Color ColorOf(string color)
        {
            return ColorOf(NumberOf(color));
        }

        public string NameOf(int color)
        {
            return color switch
            {
                0 => "orange",
                1 => "red",
                2 => "blue",
                3 => "green",
                4 => "purple",
                5 => "navy",
                6 => "teal",
                7 => "brown",
                8 => "pink",
                9 => "yellow",
                _ => "blank"
            };
        }

        public int NumberOf(string color)
        {
            return color switch
            {
                "orange" => 0,
                "red" => 1,
                "blue" => 2,
                "green" => 3,
                "purple" => 4,
                "navy" => 5,
                "teal" => 6,
                "brown" => 7,
                "pink" => 8,
                "yellow" => 9,
                _ => -1
            };
        }
    }
}
