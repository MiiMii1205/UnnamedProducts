using PEAKLib.Core;
using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class AntifreezeExplosionController: MonoBehaviour
{
    public GameObject explosionPrefab = null!;
    public GameObject stickyFireball = null!;
    public int m_amountOfFireballs = 5;

    private void Start()
    {
        Instantiate(explosionPrefab);

        var exp = Instantiate(explosionPrefab, transform.position, transform.rotation);

        exp.SetActive(true);

        for (int i = 0; i < m_amountOfFireballs; i++)
        {
            var launchAngle = Quaternion.Euler(360f, 360f / m_amountOfFireballs * (i + 1), 11 * 30f);

            var offset = (Vector3.ProjectOnPlane(
                launchAngle * (Vector3.up * 5), Vector3.up).normalized * 0.125f) + (Vector3.up * 0.0625f);
            
            var fireball =
                NetworkPrefabManager.SpawnNetworkPrefab(stickyFireball.name, exp.transform.position + offset, Quaternion.identity);
            
            fireball.gameObject.SetActive(true);
            fireball.GetComponent<Rigidbody>().AddForce(launchAngle * (Vector3.up * 5), ForceMode.VelocityChange);
        }
    }
}