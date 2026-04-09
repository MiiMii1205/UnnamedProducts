using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class LiquidController: MonoBehaviour
{
    private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");
    private static readonly int WobbleX = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZ = Shader.PropertyToID("_WobbleZ");

    [SerializeField] public float MaxWobble = 0.03f;
    [SerializeField] public float WobbleSpeedMove = 1f;
    [SerializeField] public float fillAmount = 0.25f;
    [SerializeField] public float Recovery = 1f;
    [SerializeField] public float Thickness = 1f;
    [Range(0, 1)]
    public float compensateShapeAmount;
    [SerializeField] public Mesh mesh;
    [SerializeField] public Renderer rend;
    
    private Vector3 m_pos;
    private Vector3 m_lastPos;
    private Vector3 m_velocity;
    private Quaternion m_lastRot;
    private Vector3 m_angularVelocity;
    private float m_wobbleAmountX;
    private float m_wobbleAmountZ;
    private float m_wobbleAmountToAddX;
    private float m_wobbleAmountToAddZ;
    private float m_pulse;
    private float m_sinewave;
    private float m_time = 0.5f;
    private Vector3 m_comp;
 
    private void Start()
    {
        GetMeshAndRend();
    }
 
    private void OnValidate()
    {
        GetMeshAndRend();
    }

    private void GetMeshAndRend()
    {
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }

    private void Update()
    {
        m_time += Time.deltaTime;
 
        if (Time.deltaTime != 0)
        {
            // decrease wobble over time
            m_wobbleAmountToAddX = Mathf.Lerp(m_wobbleAmountToAddX, 0, (Time.deltaTime * Recovery));
            m_wobbleAmountToAddZ = Mathf.Lerp(m_wobbleAmountToAddZ, 0, (Time.deltaTime * Recovery));
            
            // make a sine wave of the decreasing wobble
            m_pulse = 2 * Mathf.PI * WobbleSpeedMove;
            m_sinewave = Mathf.Lerp(m_sinewave, Mathf.Sin(m_pulse * m_time), Time.deltaTime * Mathf.Clamp(m_velocity.magnitude + m_angularVelocity.magnitude, Thickness, 10));
 
            m_wobbleAmountX = m_wobbleAmountToAddX * m_sinewave;
            m_wobbleAmountZ = m_wobbleAmountToAddZ * m_sinewave;
            
            // velocity
            m_velocity = (m_lastPos - transform.position) / Time.deltaTime;
            m_angularVelocity = GetAngularVelocity(m_lastRot, transform.rotation);
 
            // add clamped velocity to wobble
            m_wobbleAmountToAddX += Mathf.Clamp((m_velocity.x + (m_velocity.y * 0.2f) + m_angularVelocity.z + m_angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
            m_wobbleAmountToAddZ += Mathf.Clamp((m_velocity.z + (m_velocity.y * 0.2f) + m_angularVelocity.x + m_angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
        }
 
        // send it to the shader
        rend.sharedMaterial.SetFloat(WobbleX, m_wobbleAmountX);
        rend.sharedMaterial.SetFloat(WobbleZ, m_wobbleAmountZ);
 
        // set fill amount
        UpdatePos();
 
        // keep last position
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;
    }

    private void UpdatePos()
    {
        var worldPos = transform.TransformPoint(mesh.bounds.center);
        
        if (compensateShapeAmount > 0)
        {
            // only lerp if not paused/normal update
            if (Time.deltaTime != 0)
            {
                m_comp = Vector3.Lerp(m_comp, (worldPos - new Vector3(0, GetLowestPoint(), 0)), Time.deltaTime * 10);
            }
            else
            {
                m_comp = (worldPos - new Vector3(0, GetLowestPoint(), 0));
            }
 
            m_pos = worldPos - transform.position - new Vector3(0, fillAmount - (m_comp.y * compensateShapeAmount), 0);
        }
        else
        {
            m_pos = worldPos - transform.position - new Vector3(0, fillAmount, 0);
        }
        rend.sharedMaterial.SetVector(FillAmount, m_pos);
    }
 
    //https://forum.unity.com/threads/manually-calculate-angular-velocity-of-gameobject.289462/#post-4302796
    private static Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        
        // no rotation?
        // You may want to increase this closer to 1 if you want to handle very small rotations.
        // Beware, if it is too close to one your answer will be Nan
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
        {
            return Vector3.zero;
        }

        float gain;
        // handle negatives, we could just flip it but this is faster
        if (q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        
        var angularVelocity = new Vector3(q.x * gain, q.y * gain, q.z * gain);
 
        if (float.IsNaN(angularVelocity.z))
        {
            angularVelocity = Vector3.zero;
        }
        
        return angularVelocity;
    }

    private float GetLowestPoint()
    {
        var lowestY = float.MaxValue;
        var lowestVert = Vector3.zero;
        var vertices = mesh.vertices;
        
        for (int i = 0, l = vertices.Length; i < l; ++i)
        {
            var position = transform.TransformPoint(vertices[i]);
 
            if (position.y < lowestY)
            {
                lowestY = position.y;
                lowestVert = position;
            }
        }
        
        return lowestVert.y;
    }
}