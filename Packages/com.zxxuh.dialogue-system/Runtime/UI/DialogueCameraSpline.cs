using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.UI
{
    public sealed class DialogueCameraSpline : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private List<Transform> controlPoints = new List<Transform>();
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(4)] private int gizmoSamplesPerSegment = 16;

        private int currentControlPointIndex = -1;

        public int ControlPointCount => controlPoints == null ? 0 : controlPoints.Count;

        public int CurrentControlPointIndex => currentControlPointIndex;

        public void Configure(Camera camera, IReadOnlyList<Transform> points)
        {
            targetCamera = camera;
            controlPoints = points == null ? new List<Transform>() : new List<Transform>(points);
        }

        public void SnapToControlPoint(int index)
        {
            ValidateCameraAndPoint(index);
            targetCamera.transform.SetPositionAndRotation(
                controlPoints[index].position,
                controlPoints[index].rotation);
            currentControlPointIndex = index;
        }

        public IEnumerator MoveToControlPoint(int targetIndex, float duration)
        {
            ValidateCameraAndPoint(targetIndex);

            // 未初始化时没有可靠的曲线起点，直接定位比猜测当前段更安全。
            if (currentControlPointIndex < 0 || duration <= 0f)
            {
                SnapToControlPoint(targetIndex);
                yield break;
            }

            var fromIndex = currentControlPointIndex;
            var fromRotation = controlPoints[fromIndex].rotation;
            var toRotation = controlPoints[targetIndex].rotation;
            var positions = CapturePositions();
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var eased = movementCurve == null ? normalized : movementCurve.Evaluate(normalized);
                var position = DialogueSplineMath.EvaluateSegment(positions, fromIndex, targetIndex, eased);
                var rotation = Quaternion.Slerp(fromRotation, toRotation, eased);
                targetCamera.transform.SetPositionAndRotation(position, rotation);
                yield return null;
            }

            // 浮点累计可能无法精确落在终点，最后强制对齐以避免多步骤产生漂移。
            SnapToControlPoint(targetIndex);
        }

        private List<Vector3> CapturePositions()
        {
            var positions = new List<Vector3>(ControlPointCount);
            foreach (var point in controlPoints)
            {
                if (point == null)
                {
                    throw new InvalidOperationException("镜头路径包含空控制点引用。");
                }

                positions.Add(point.position);
            }

            return positions;
        }

        private void ValidateCameraAndPoint(int index)
        {
            // 明确报告引用和索引，方便样例被复制后快速定位 Inspector 配置问题。
            if (targetCamera == null)
            {
                throw new InvalidOperationException("DialogueCameraSpline 未绑定目标相机。");
            }

            if (controlPoints == null || index < 0 || index >= controlPoints.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "镜头路径控制点索引无效。");
            }

            if (controlPoints[index] == null)
            {
                throw new InvalidOperationException($"镜头路径控制点 {index} 为空。");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (controlPoints == null || controlPoints.Count == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.9f);
            foreach (var point in controlPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.15f);
                }
            }

            // Gizmos 仅用于编辑路径，不会向运行时场景增加任何可见几何体。
            try
            {
                var positions = CapturePositions();
                for (var segment = 0; segment < positions.Count - 1; segment++)
                {
                    var previous = positions[segment];
                    var samples = Mathf.Max(4, gizmoSamplesPerSegment);
                    for (var sample = 1; sample <= samples; sample++)
                    {
                        var current = DialogueSplineMath.EvaluateSegment(
                            positions,
                            segment,
                            segment + 1,
                            sample / (float)samples);
                        Gizmos.DrawLine(previous, current);
                        previous = current;
                    }
                }
            }
            catch (Exception)
            {
                // Inspector 正在编辑空引用时跳过绘制，实际运行仍由 ValidateCameraAndPoint 报错。
            }
        }
    }
}
