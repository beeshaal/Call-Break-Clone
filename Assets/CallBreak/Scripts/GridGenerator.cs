using UnityEngine;
using System.Collections.Generic;

public class GridArranger : MonoBehaviour
{
    [Header("Grid Arrangement")]
    public List<GameObject> objectsToArrange = new List<GameObject>();
    public int columns = 3;
    public float spacingX = 2f;
    public float spacingZ = 2f;
    public bool centerPivot = true;

    [ContextMenu("Arrange Objects In Grid")]
    public void ArrangeGrid()
    {
        if (objectsToArrange == null || objectsToArrange.Count == 0)
        {
            Debug.LogWarning("No objects in the list to arrange!");
            return;
        }

        int totalObjects = objectsToArrange.Count;
        int rows = Mathf.CeilToInt((float)totalObjects / columns);

        Vector3 startOffset = Vector3.zero;
        if (centerPivot)
        {
            float totalWidth = (columns - 1) * spacingX;
            float totalDepth = (rows - 1) * spacingZ;
            startOffset = new Vector3(-totalWidth / 2f, 0, -totalDepth / 2f);
        }

        for (int i = 0; i < totalObjects; i++)
        {
            int row = i / columns;
            int col = i % columns;

            Vector3 position = new Vector3(col * spacingX, 0, row * spacingZ) + startOffset;
            GameObject obj = objectsToArrange[i];

            if (obj != null)
            {
                obj.transform.SetParent(transform);
                obj.transform.localPosition = position;
            }
        }
    }
}
