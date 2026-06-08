using System.Reflection;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    internal class ButtonPopupField : BaseField<string>
    {
        private static readonly PropertyInfo s_VirtualInputProperty;
        private static readonly PropertyInfo s_ElementPanelProperty;
        private static readonly MethodInfo   s_GetTopElementUnderPointerMethod;

        private readonly TextElement   m_TextElement;
        private          VisualElement m_ArrowElement;

        public event System.Action clicked;

        [CreateProperty]
        public string text
        {
            get => m_TextElement.text;
            set
            {
                if (value == m_TextElement.text)
                    return;
                SetValueWithoutNotify(value);
                using var evt = ChangeEvent<string>.GetPooled(m_TextElement.text, value);
                evt.target = this;
                SendEvent(evt);
            }
        }

        private VisualElement m_VirtualInput
        {
            get => (VisualElement) s_VirtualInputProperty.GetValue(this);
            set => s_VirtualInputProperty.SetValue(this, value);
        }

        private IPanel m_ElementPanel => (IPanel) s_ElementPanelProperty.GetValue(this);

        static ButtonPopupField()
        {
            s_VirtualInputProperty            = typeof(BaseField<string>).GetProperty("visualInput", BindingFlags.NonPublic | BindingFlags.Instance);
            s_ElementPanelProperty            = typeof(VisualElement).GetProperty("elementPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            s_GetTopElementUnderPointerMethod = s_ElementPanelProperty.PropertyType.GetMethod("GetTopElementUnderPointer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public ButtonPopupField(string label) : base(label, null)
        {
            AddToClassList(BasePopupField<string, string>.ussClassName);
            labelElement.AddToClassList(BasePopupField<string, string>.labelUssClassName);
            PopupTextElement popupTextElement = new PopupTextElement
            {
                pickingMode = PickingMode.Ignore
            };
            m_TextElement = popupTextElement;
            m_TextElement.AddToClassList(BasePopupField<string, string>.textUssClassName);
            m_VirtualInput.AddToClassList(BasePopupField<string, string>.inputUssClassName);
            m_VirtualInput.Add(m_TextElement);
            m_ArrowElement = new VisualElement();
            m_ArrowElement.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
            m_ArrowElement.pickingMode = PickingMode.Ignore;
            m_VirtualInput.Add(m_ArrowElement);
            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0)
                    return;
                e.StopPropagation();
            });
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ((INotifyValueChanged<string>) m_TextElement).SetValueWithoutNotify(newValue);
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            ProcessPointerDown(evt);
        }

        private void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (evt.button != 0 || !ContainsPointer(evt.pointerId))
                return;
            evt.StopPropagation();
        }

        private void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (evt.button != 0 || (evt.pressedButtons & 1) == 0)
                return;
            ProcessPointerDown(evt);
        }

        private void ProcessPointerDown<T>(PointerEventBase<T> evt) where T : PointerEventBase<T>, new()
        {
            if (evt.button != 0 || !ContainsPointer(evt.pointerId))
                return;
            schedule.Execute(clicked);
            evt.StopPropagation();
        }

        private bool ContainsPointer(int pointerId)
        {
            VisualElement elementUnderPointer = GetTopElementUnderPointer(pointerId);
            return this == elementUnderPointer || m_VirtualInput == elementUnderPointer;
        }

        private VisualElement GetTopElementUnderPointer(int pointerId)
        {
            return (VisualElement) s_GetTopElementUnderPointerMethod.Invoke(m_ElementPanel, new object[] {pointerId});
        }

        private class PopupTextElement : TextElement
        {
            protected override UnityEngine.Vector2 DoMeasure(
                float desiredWidth,
                MeasureMode widthMode,
                float desiredHeight,
                MeasureMode heightMode)
            {
                var textToMeasure = text;
                if (string.IsNullOrEmpty(textToMeasure))
                    textToMeasure = " ";
                return MeasureTextSize(textToMeasure, desiredWidth, widthMode, desiredHeight, heightMode);
            }
        }
    }
}