using System;
using Extensions;
using Extensions.Valheim;
using UnityEngine;
using static ChunkLoader.Plugin;

namespace ChunkLoader;

public class ChunkLoaderMono : MonoBehaviour, Hoverable, Interactable
{
    public const string m_name = "$piece_ChunkLoader_stone";
    private static readonly float disabledEmission = 6;
    public ZNetView m_nview;
    public Piece m_piece;

    public static float m_startFuel = 1f;
    public static float m_maxFuel = 100f;
    public static bool m_infiniteFuel;
    public static ItemDrop m_fuelItem;

    public EffectList m_fuelAddedEffects = new();

    public float m_holdRepeatInterval = 0.2f;
    public float m_lastUseTime;
    private Renderer m_renderer;
    private Color matColor = Color.clear;

    public static int minutesForOneFuelItem = 5;

    public void Awake()
    {
        m_fuelItem = ObjectDB.instance.GetItem("Thunderstone");
        m_nview = gameObject.GetComponent<ZNetView>();
        m_piece = gameObject.GetComponent<Piece>();
        if (m_nview.GetZDO() == null) return;
        if (m_nview.IsOwner() && m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, -1f) == -1)
        {
            m_nview.GetZDO().Set(ZDOVars.s_fuel, m_startFuel);
            if (m_startFuel > 0) m_fuelAddedEffects.Create(transform.position, transform.rotation);
        }

        m_nview.Register("AddFuel", RPC_AddFuel);
        InvokeRepeating(nameof(UpdateFireplace), 0, 2f);
        m_renderer = transform.FindChildByName("SM_cloumn").GetComponent<Renderer>();
        if (m_renderer && m_renderer.material.HasProperty("_EmissionColor"))
            matColor = m_renderer.material.GetColor("_EmissionColor");
    }

    public string GetHoverText()
    {
        if (!m_nview.IsValid() || m_infiniteFuel) return string.Empty;
        return Localization.instance.Localize(m_name + " ( $piece_fire_fuel "
                                                     + Mathf.Ceil(m_nview.GetZDO().GetFloat(ZDOVars.s_fuel)) + "/"
                                                     + ((int)m_maxFuel)
                                                     + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use "
                                                     + m_fuelItem.m_itemData.m_shared.m_name
                                                     + "\n[<color=yellow><b>1-8</b></color>] $piece_useitem");
    }

    public string GetHoverName() { return m_name; }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold && (m_holdRepeatInterval <= 0.0 || Time.time - m_lastUseTime < m_holdRepeatInterval))
            return false;
        if (!m_nview.HasOwner()) m_nview.ClaimOwnership();
        var inventory = user.GetInventory();
        if (inventory == null) return true;
        if (m_infiniteFuel) return false;
        if (inventory.HaveItem(m_fuelItem.m_itemData.m_shared.m_name))
        {
            if (Mathf.CeilToInt(m_nview.GetZDO().GetFloat(ZDOVars.s_fuel)) >= m_maxFuel)
            {
                user.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_cantaddmore", m_fuelItem.m_itemData.m_shared.m_name));
                return false;
            }

            user.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$msg_fireadding", m_fuelItem.m_itemData.m_shared.m_name));
            inventory.RemoveItem(m_fuelItem.m_itemData.m_shared.m_name, 1);
            m_nview.InvokeRPC("AddFuel");
            return true;
        }

        user.Message(MessageHud.MessageType.Center, "$msg_outof " + m_fuelItem.m_itemData.m_shared.m_name);
        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (item.m_shared.m_name == m_fuelItem.m_itemData.m_shared.m_name && !m_infiniteFuel)
        {
            if (Mathf.CeilToInt(m_nview.GetZDO().GetFloat(ZDOVars.s_fuel)) >= m_maxFuel)
            {
                user.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_cantaddmore", item.m_shared.m_name));
                return true;
            }

            var inventory = user.GetInventory();
            user.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$msg_fireadding", item.m_shared.m_name));
            inventory.RemoveItem(item, 1);
            m_nview.InvokeRPC("AddFuel");
            return true;
        }

        return false;
    }

    public void UpdateState()
    {
        var zone = ZoneSystem.instance.GetZone(transform.position);
        if (IsBurning())
        {
            if (m_renderer && matColor != Color.clear && m_renderer.material.HasProperty("_EmissionColor"))
                m_renderer.material.SetColor("_EmissionColor", matColor);

            if (!ForceActive.Contains(zone)) ForceActive.Add(zone);
            if (!ForceActiveBuffer.Contains(zone)) ForceActiveBuffer.Add(zone);
        } else
        {
            if (m_renderer && m_renderer.material.HasProperty("_EmissionColor"))
                m_renderer.material.SetColor("_EmissionColor", Color.red * disabledEmission);

            if (ForceActive.Contains(zone)) ForceActive.Remove(zone);
            if (ForceActiveBuffer.Contains(zone)) ForceActiveBuffer.Remove(zone);
        }
    }

    public bool CanBeRemoved() { return !IsBurning(); }

    public bool IsBurning() { return m_infiniteFuel || m_nview.GetZDO().GetFloat(ZDOVars.s_fuel) > 0.0; }

    public void RPC_AddFuel(long sender)
    {
        if (!m_nview.IsOwner()) return;
        var f = m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
        if (Mathf.CeilToInt(f) >= m_maxFuel) return;
        var num = Mathf.Clamp(Mathf.Clamp(f, 0, m_maxFuel) + 1f, 0, m_maxFuel);
        m_nview.GetZDO().Set(ZDOVars.s_fuel, num);
        m_fuelAddedEffects.Create(transform.position, transform.rotation);
        UpdateState();
    }

    public void UpdateFireplace()
    {
        if (!m_nview.IsValid()) return;
        var secPerFuel = TimeSpan.FromMinutes(minutesForOneFuelItem).TotalSeconds;
        if (m_nview.IsOwner() && secPerFuel > 0)
        {
            var savedFuel = m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
            var timeSinceLastUpdate = GetTimeSinceLastUpdate();
            if (IsBurning() && !m_infiniteFuel)
            {
                var fuelSpent = timeSinceLastUpdate / secPerFuel;
                var newFuel = savedFuel - fuelSpent;
                if (newFuel <= 0.0) newFuel = 0;
                m_nview.GetZDO().Set(ZDOVars.s_fuel, (float)newFuel);
            }
        }

        UpdateState();
    }

    public double GetTimeSinceLastUpdate()
    {
        var time = ZNet.instance.GetTime();
        DateTime dateTime = new(m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, time.Ticks));
        var timeSpan = time - dateTime;
        m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
        var timeSinceLastUpdate = timeSpan.TotalSeconds;
        if (timeSinceLastUpdate < 0) timeSinceLastUpdate = 0;
        return timeSinceLastUpdate;
    }
}