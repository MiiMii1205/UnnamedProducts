using PEAKLib.Core;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedSpawnable : MonoBehaviour
{
    public GameObject prefabToSpawn;
    
    private void Start()
    {
        var exp = Instantiate(prefabToSpawn);
        exp.SetActive(true);
    }
}