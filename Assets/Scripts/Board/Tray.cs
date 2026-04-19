using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Tray
{
    private Item[] m_slots;
    private Transform[] m_slotTransforms;
    private Transform m_root;
    private int m_traySize;
    private int m_matchMin;
    private int m_count;

    public bool IsFull => m_count >= m_traySize;

    public Tray(Transform root, GameSettings gameSettings)
    {
        m_root = root;
        m_traySize = gameSettings.traySize;
        m_matchMin = gameSettings.MatchesMin;

        m_slots = new Item[m_traySize];
        m_slotTransforms = new Transform[m_traySize];

        CreateSlots(gameSettings.BoardSizeY);
    }

    private void CreateSlots(int boardSizeY)
    {
        float trayY = -boardSizeY * 0.5f - 1.5f;
        float startX = -(m_traySize - 1) * 0.5f;

        GameObject prefab = Resources.Load<GameObject>(Constants.PREFAB_TRAY_SLOT);

        for (int i = 0; i < m_traySize; i++)
        {
            GameObject go = GameObject.Instantiate(prefab);
            go.transform.position = new Vector3(startX + i, trayY, 0f);
            go.transform.SetParent(m_root);
            m_slotTransforms[i] = go.transform;
        }
    }

    public bool AddItem(Item item)
    {
        if (IsFull) return false;

        int index = GetNextOpenIndex();
        if (index < 0) return false;

        m_slots[index] = item;
        m_count++;

        // Tag the view so it can be identified by raycast
        TrayItemView tag = item.View.gameObject.AddComponent<TrayItemView>();
        tag.Item = item;

        // Ensure it has a collider for raycasting
        if (item.View.GetComponent<Collider2D>() == null)
            item.View.gameObject.AddComponent<BoxCollider2D>();

        item.View.DOMove(m_slotTransforms[index].position, 0.3f).OnComplete(() =>
        {
            CheckAndClearMatches();
        });

        return true;
    }

    private int GetNextOpenIndex()
    {
        for (int i = 0; i < m_traySize; i++)
        {
            if (m_slots[i] == null) return i;
        }
        return -1;
    }

    private void CheckAndClearMatches()
    {
        List<int> matchIndices = FindMatchIndices();
        if (matchIndices.Count == 0) return;

        foreach (int i in matchIndices)
        {
            m_slots[i].ExplodeView();
            m_slots[i] = null;
            m_count--;
        }

        CompactSlots();
    }

    private List<int> FindMatchIndices()
    {
        for (int i = 0; i <= m_traySize - 3; i++)
        {
            if (m_slots[i] == null) continue;

            if (m_slots[i + 1] != null && m_slots[i + 1].IsSameType(m_slots[i]) &&
                m_slots[i + 2] != null && m_slots[i + 2].IsSameType(m_slots[i]))
            {
                return new List<int> { i, i + 1, i + 2 };
            }
        }

        return new List<int>();
    }

    private void CompactSlots()
    {
        int writeIndex = 0;
        for (int i = 0; i < m_traySize; i++)
        {
            if (m_slots[i] == null) continue;

            if (i != writeIndex)
            {
                m_slots[writeIndex] = m_slots[i];
                m_slots[i] = null;
                m_slots[writeIndex].View.DOMove(m_slotTransforms[writeIndex].position, 0.2f);
            }
            writeIndex++;
        }
    }

    public Item ReturnItem(TrayItemView tag)
    {
        Item target = tag.Item;

        for (int i = 0; i < m_traySize; i++)
        {
            if (m_slots[i] != target) continue;

            m_slots[i] = null;
            m_count--;

            Object.Destroy(tag);
            Collider2D col = target.View.GetComponent<Collider2D>();
            if (col != null) Object.Destroy(col);

            CompactSlots();
            return target;
        }
        return null;
    }

    public bool HasAnyMatch()
    {
        return FindMatchIndices().Count >= m_matchMin;
    }

    public void Clear()
    {
        for (int i = 0; i < m_traySize; i++)
        {
            if (m_slots[i] != null)
            {
                m_slots[i].Clear();
                m_slots[i] = null;
            }
        }

        foreach (Transform t in m_slotTransforms)
        {
            if (t != null) GameObject.Destroy(t.gameObject);
        }
    }
}
