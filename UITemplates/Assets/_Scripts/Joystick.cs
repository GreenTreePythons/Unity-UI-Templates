using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts
{
    public enum JoystickType{ Static, Dynamic, Floating }

    public enum JoystickDirectionType
    {
        None = 0, Four= 4, Eight = 8
    }
    
    [AddComponentMenu("UITemplates/Joystick")] 
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class Joystick : MonoBehaviour, 
        IPointerDownHandler, 
        IPointerUpHandler, 
        IDragHandler 
    {
        [Header("References")]
        [SerializeField] RectTransform m_JoystickBGRectTransform;
        [SerializeField] RectTransform m_JoystickHandleRectTransform;
        
        [Header("Settings")]
        [SerializeField] JoystickType m_JoystickType = JoystickType.Static;
        [SerializeField] float m_JoystickHandleMoveRange = 55f;
        [Range(0f, 1f)] 
        [SerializeField] float m_JoystickDeadZone = 0.1f;

        [Tooltip("Set 0 or 1 for free handle, or set according to number of direction need like 4 or 8")]
        [SerializeField] JoystickDirectionType m_JoystickDirectionType = JoystickDirectionType.None;
        
        private int m_JoystickHandleDirectionCount = 0;
        private Vector2 m_InputDirection = Vector2.zero;
        private Vector2 m_JoystickBGStartPos;
        private Canvas m_Canvas;
        private bool m_DragStartedInside = false;

        private void Awake()
        {
            m_JoystickHandleDirectionCount = (int)m_JoystickDirectionType;
            m_JoystickBGStartPos = m_JoystickBGRectTransform.anchoredPosition;
            m_Canvas = GetComponent<Canvas>();
            if (m_JoystickType != JoystickType.Static)
            {
                m_JoystickBGRectTransform.gameObject.SetActive(false);
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            m_JoystickBGRectTransform.gameObject.SetActive(true);
            
            Vector2 touchPos = GetTouchPosition(m_Canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera);
            
            if (m_JoystickType != JoystickType.Static)
            {
                m_JoystickBGRectTransform.anchoredPosition = touchPos;
            }
            
            m_DragStartedInside = touchPos.magnitude <= m_JoystickHandleMoveRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_JoystickHandleRectTransform.anchoredPosition = Vector2.zero;
            m_InputDirection = Vector2.zero;

            if (m_JoystickType == JoystickType.Floating || m_JoystickType == JoystickType.Dynamic)
            {
                m_JoystickBGRectTransform.anchoredPosition = m_JoystickBGStartPos;
            }

            if (m_JoystickType != JoystickType.Static)
            {
                m_JoystickBGRectTransform.gameObject.SetActive(false);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_JoystickType == JoystickType.Static && !m_DragStartedInside) return;
            
            Vector2 touchPos = GetTouchPosition(m_JoystickBGRectTransform, eventData.position, eventData.pressEventCamera);
            Vector2 clamped = Vector2.ClampMagnitude(touchPos, m_JoystickHandleMoveRange);
            m_JoystickHandleRectTransform.anchoredPosition = clamped;
            
            Vector2 rawInput = clamped / m_JoystickHandleMoveRange;
            m_InputDirection = rawInput.magnitude < m_JoystickDeadZone ? Vector2.zero : rawInput;
            
            if (m_JoystickHandleDirectionCount > 1 && m_InputDirection != Vector2.zero)
            {
                float angle = Vector2.SignedAngle(Vector2.left, m_InputDirection);
                float snapAngle = 360f / m_JoystickHandleDirectionCount;
                float snappedAngle = Mathf.Round(angle / snapAngle) * snapAngle;

                m_InputDirection = Quaternion.Euler(0, 0, snappedAngle) * Vector2.left;
                m_JoystickHandleRectTransform.anchoredPosition = m_InputDirection * m_JoystickHandleMoveRange;
            }

            if (m_JoystickType == JoystickType.Floating && touchPos.magnitude > m_JoystickHandleMoveRange)
            {
                Vector2 offset = touchPos.normalized * (touchPos.magnitude - m_JoystickHandleMoveRange);
                m_JoystickBGRectTransform.anchoredPosition += offset;
            }
        }

        private Vector2 GetTouchPosition(RectTransform mainCanvasRect, Vector2 touchPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mainCanvasRect, touchPoint, eventCamera, out Vector2 getTouchPosition);
            return getTouchPosition;
        }
        
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (m_JoystickBGRectTransform != null)
            {
                Vector3 baseWorldPos = m_JoystickBGRectTransform.position;

                float angleStep = 360f / m_JoystickHandleDirectionCount;

                Handles.color = new Color(1f, 1f, 0f, 0.6f);
                for (int i = 0; i < m_JoystickHandleDirectionCount; i++)
                {
                    float angle = i * angleStep;
                    Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.left;

                    Vector3 start = baseWorldPos + (Vector3)(dir * (m_JoystickHandleMoveRange - 10f));
                    Vector3 end = baseWorldPos + (Vector3)(dir * (m_JoystickHandleMoveRange + 4f));

                    Handles.DrawLine(start, end);
                }

                // handle range
                Color cyanCol = Color.cyan;
                Handles.color = new(cyanCol.r, cyanCol.g, cyanCol.b, 0.03f);
                Handles.DrawSolidDisc(baseWorldPos, Vector3.forward, m_JoystickHandleMoveRange);
                Handles.color = cyanCol;
                Handles.DrawWireDisc(baseWorldPos, Vector3.forward, m_JoystickHandleMoveRange);

                // dead zone
                Handles.color = Color.red;
                Handles.DrawWireDisc(baseWorldPos, Vector3.forward, m_JoystickHandleMoveRange * m_JoystickDeadZone);

                // direction output
                Handles.color = Color.white;
                Handles.DrawLine(baseWorldPos, baseWorldPos + ((Vector3)m_InputDirection * m_JoystickHandleMoveRange));
            }
        }
#endif
    }
}