using UnityEngine;
using UnityEngine.EventSystems;

namespace LogisticsTycoon.ClipBoard
{
    /// <summary>
    /// 알바 이름표. 드래그하면 고스트가 포인터를 따라다니고, 대기실/작업 구역 위에 놓으면 배치된다.
    /// 드래그 이벤트를 이 컴포넌트가 받으므로 바깥 ScrollRect는 스크롤되지 않는다.
    /// </summary>
    public class StaffChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public ClipboardController controller;
        public StaffData staff;

        public void OnBeginDrag(PointerEventData e) => controller.BeginChipDrag(this, e.position);
        public void OnDrag(PointerEventData e) => controller.DragChip(this, e.position);
        public void OnEndDrag(PointerEventData e) => controller.EndChipDrag(this, e.position);
        public void OnPointerEnter(PointerEventData e) => controller.HoverChip(this, true);
        public void OnPointerExit(PointerEventData e) => controller.HoverChip(this, false);
    }
}
