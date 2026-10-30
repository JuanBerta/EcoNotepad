using System;
using System.Collections.Generic;

namespace EcoNotepad
{
    /// <summary>
    /// A market store with optional world coordinates and a per-item price table.
    /// Persisted to stores.json next to notes.json.
    /// </summary>
    public class StoreInfo
    {
        public string Name;
        public double X, Y, Z;
        public bool HasCoords;
        public Dictionary<string, double> Prices;

        public StoreInfo()
        {
            Name = "";
            HasCoords = false;
            Prices = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Returns the price for an item, or -1 if the store has no price for it.</summary>
        public double GetPrice(string item)
        {
            if (item == null) return -1;
            double v;
            return Prices.TryGetValue(item.Trim(), out v) ? v : -1;
        }

        /// <summary>Counts how many of the given items this store has a price for.</summary>
        public int CountItems(IEnumerable<string> items)
        {
            int n = 0;
            if (items == null) return 0;
            foreach (string it in items)
            {
                if (!string.IsNullOrEmpty(it) && Prices.ContainsKey(it)) n++;
            }
            return n;
        }

        /// <summary>Horizontal (X/Z) distance from a player position.</summary>
        public double DistanceTo(double px, double pz)
        {
            double dx = X - px;
            double dz = Z - pz;
            return Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
