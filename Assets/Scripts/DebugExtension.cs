// Assets/Scripts/DebugExtension.cs
using UnityEngine;

/// <summary>Quelques helpers Debug.Draw… (runtime + éditeur)</summary>
public static class DebugExtension
{
    /// <summary>Dessine un cercle dans le plan donné par la normale.</summary>
    /// <param name="center">centre monde</param>
    /// <param name="normal">vecteur normal (p.ex. Vector3.up)</param>
    /// <param name="color">couleur</param>
    /// <param name="radius">rayon</param>
    /// <param name="segments">nb de segments (déf. 32)</param>
    /// <param name="duration">durée (0 = une frame)</param>
    public static void DrawCircle(
        Vector3 center,
        Vector3 normal,
        Color   color,
        float   radius,
        int     segments = 32,
        float   duration = 0f)
    {
        if (segments < 3) segments = 3;

        // Trouve deux axes perpendiculaires dans le plan voulu
        Vector3 axisA = Vector3.Cross(normal, Vector3.up);
        if (axisA.sqrMagnitude < 0.01f) axisA = Vector3.Cross(normal, Vector3.right);
        axisA.Normalize();
        Vector3 axisB = Vector3.Cross(normal, axisA);

        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + axisA * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + (Mathf.Cos(angle) * axisA + Mathf.Sin(angle) * axisB) * radius;
            Debug.DrawLine(prev, next, color, duration);
            prev = next;
        }
    }
}