using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

[RequireComponent(typeof(PhotonView))]
public class UnnamedParasol : Parasol
{
    private float m_floatingTime;
    private float m_baseCloseTime;
    private float m_closeTime;
    private bool m_isInverted;

    private void Start()
    {
        m_isInverted = item.GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1), DefaultInverted)
            .Value;
        m_baseCloseTime = item.GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey), DefaultCloseTime)
            .Value;
        
        UnnamedPlugin.Log.LogInfo(
            $"{nameof(UnnamedParasol)} {gameObject.name} is {(m_isInverted ? "inverted" : $"normal and will invert after {m_baseCloseTime} seconds floating" )}");
        
        CloseTimeModifier = UnnamedPlugin.RandomUnnamedModifier;

        if (m_isInverted)
        {
            InstantInvertParasol();
        }
    }

    private void InstantInvertParasol()
    {
        var parasolOpen = item.transform.GetChild(2).GetChild(1).GetChild(0);
        var parasolClosed = item.transform.GetChild(2).GetChild(2).GetChild(0);

        var endLocalScaleOpen = new Vector3(parasolOpen.localScale.x, parasolOpen.localScale.y,
            -Mathf.Abs(parasolOpen.localScale.z));

        var endLocalScaleClosed = new Vector3(parasolClosed.localScale.x, parasolClosed.localScale.y,
            -Mathf.Abs(parasolClosed.localScale.z));


        var endLocalPositionOpen = new Vector3(parasolOpen.localPosition.x, Mathf.Abs(parasolOpen.localPosition.y),
            parasolOpen.localPosition.z);

        var endLocalPositionClosed = new Vector3(parasolClosed.localPosition.x, Mathf.Abs(parasolClosed.localPosition.y),
            parasolClosed.localPosition.z);
        
        parasolOpen.localScale = endLocalScaleOpen;
        parasolClosed.localScale = endLocalScaleClosed;
        parasolOpen.localPosition = endLocalPositionOpen;
        parasolClosed.localPosition = endLocalPositionClosed;
    }

    public float CloseTimeModifier
    {
        set => m_closeTime = m_baseCloseTime * value;
    }

    private BoolItemData DefaultInverted()
    {
        return new BoolItemData()
        {
            Value = false
        };
    }


    private FloatItemData DefaultCloseTime()
    {
        return new FloatItemData()
        {
            Value = 5.0f * UnnamedPlugin.RandomUnnamedModifier,
        };
    }

    [PunRPC]
    public new void ToggleOpenRPC(bool open)
    {
        base.ToggleOpenRPC(open);
        m_floatingTime = 0f;
    }

    [PunRPC]
    public void InvertParasolRPC()
    {
        m_isInverted = true;

        item.GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1), DefaultInverted)
            .Value = m_isInverted;

        UnnamedPlugin.Log.LogInfo(
            $"{nameof(UnnamedParasol)} {gameObject.name} just inverted!");
        
        StartCoroutine(InvertParasolTop());
    }

    private IEnumerator InvertParasolTop(float inversionTime = 0.75f)
    {
        var time = 0f;

        var parasolOpen = item.transform.GetChild(2).GetChild(1).GetChild(0);
        var parasolClosed = item.transform.GetChild(2).GetChild(2).GetChild(0);

        var endLocalScaleOpen = new Vector3(parasolOpen.localScale.x, parasolOpen.localScale.y,
            -Mathf.Abs(parasolOpen.localScale.z));

        var endLocalScaleClosed = new Vector3(parasolClosed.localScale.x, parasolClosed.localScale.y,
            -Mathf.Abs(parasolClosed.localScale.z));

        var startLocalScaleOpen = parasolOpen.localScale;
        var startLocalScaleClosed = parasolClosed.localScale;

        var endLocalPositionOpen = new Vector3(parasolOpen.localPosition.x, Mathf.Abs(parasolOpen.localPosition.y),
            parasolOpen.localPosition.z);

        var endLocalPositionClosed = new Vector3(parasolClosed.localPosition.x, Mathf.Abs(parasolClosed.localPosition.y),
            parasolClosed.localPosition.z);

        var startLocalPositionOpen = parasolOpen.localPosition;
        var startLocalPositionClosed = parasolClosed.localPosition;


        while (time <= inversionTime)
        {
            time += Time.deltaTime;

            parasolOpen.localScale = Vector3.Lerp(startLocalScaleOpen, endLocalScaleOpen, time / inversionTime);
            parasolClosed.localScale = Vector3.Lerp(startLocalScaleClosed, endLocalScaleClosed, time / inversionTime);

            parasolOpen.localPosition =
                Vector3.Lerp(startLocalPositionOpen, endLocalPositionOpen, time / inversionTime);
            
            parasolClosed.localPosition =
                Vector3.Lerp(startLocalPositionClosed, endLocalPositionClosed, time / inversionTime);

            yield return null;
        }

        InstantInvertParasol();
    }

    public void InvertParasol()
    {
        if (item.photonView.IsMine)
        {
            m_floatingTime = 0f;
            CloseTimeModifier = UnnamedPlugin.RandomUnnamedModifier;
        }

        item.photonView.RPC(nameof(InvertParasolRPC), RpcTarget.All);
    }

    public new void FixedUpdate()
    {
        if (item.holderCharacter && !item.holderCharacter.data.isGrounded && isOpen)
        {
            if (!m_isInverted)
            {
                item.holderCharacter.refs.movement.ApplyParasolDrag(extraYDrag, extraXZDrag);

                m_floatingTime += Time.fixedDeltaTime;

                if (m_floatingTime >= m_closeTime)
                {
                    // Goodbye
                    InvertParasol();
                }
            }
        }
    }
}