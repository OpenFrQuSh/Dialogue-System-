using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.UI
{
    public static class DialogueSplineMath
    {
        public static Vector3 EvaluateSegment(
            IReadOnlyList<Vector3> points,
            int fromIndex,
            int toIndex,
            float progress)
        {
            // 路径数据错误必须在导览开始时暴露，否则镜头会静默停在未知位置。
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException("镜头路径至少需要一个控制点。", nameof(points));
            }

            if (fromIndex < 0 || fromIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            }

            if (toIndex < 0 || toIndex >= points.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(toIndex));
            }

            var t = Mathf.Clamp01(progress);
            if (points.Count == 1 || fromIndex == toIndex)
            {
                return points[fromIndex];
            }

            // 两点无法定义曲率，退化为直线仍可保证最小样例正常运行。
            if (points.Count == 2)
            {
                return Vector3.Lerp(points[fromIndex], points[toIndex], t);
            }

            var p0 = points[Mathf.Max(0, fromIndex - 1)];
            var p1 = points[fromIndex];
            var p2 = points[toIndex];
            var p3 = points[Mathf.Min(points.Count - 1, toIndex + 1)];
            var t2 = t * t;
            var t3 = t2 * t;

            // Catmull-Rom 会穿过观察点，确保每一步结束时构图与配置完全一致。
            return 0.5f * ((2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
