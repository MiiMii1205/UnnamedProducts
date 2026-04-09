using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class UnnamedSpawnRestriction: MonoBehaviour
{
    public Biome.BiomeType[] biomeType = [];
    public bool whenNightIsCold = false;
    public bool hasColdNightRestrictions = false;
}