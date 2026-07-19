using UnityEngine;
using UnityEngine.EventSystems;

namespace SerenaysGambit
{
    public sealed class UpgradeTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private UpgradeTooltip _tooltip;
        private string _title;
        private string _description;
        private string _details;

        public void Bind(UpgradeTooltip tooltip, string title, string description, string details)
        {
            _tooltip = tooltip;
            _title = title ?? string.Empty;
            _description = description ?? string.Empty;
            _details = details ?? string.Empty;

            if (_tooltip != null)
            {
                _tooltip.Refresh(this, _title, _description, _details);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip != null)
            {
                _tooltip.Show(this, _title, _description, _details, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null)
            {
                _tooltip.Hide(this);
            }
        }

        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide(this);
            }
        }
    }
}
