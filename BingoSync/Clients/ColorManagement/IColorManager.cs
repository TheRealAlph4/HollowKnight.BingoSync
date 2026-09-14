using System.Collections.Generic;
using UnityEngine;

namespace BingoSync.Clients.ColorManagement
{
    public interface IColorManager
    {
        List<int> Colors { get; }
        string NameOf(int color);
        int NumberOf(string color);
        Color ColorOf(int color);
        Color ColorOf(string color);
    }
}
