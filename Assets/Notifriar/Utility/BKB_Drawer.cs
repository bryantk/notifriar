using UnityEngine;

static public class BKB_Drawer {

    static public void DrawCross(Vector3 position, float scale) {
        Vector3 upDown = Vector3.up * (scale / 2);
        Gizmos.DrawLine(position + upDown, position - upDown);
        Vector3 leftRight = Vector3.right * (scale / 2);
        Gizmos.DrawLine(position + leftRight, position - leftRight);
        Gizmos.DrawWireCube(position, Vector3.one * scale / 2);
    }
}
