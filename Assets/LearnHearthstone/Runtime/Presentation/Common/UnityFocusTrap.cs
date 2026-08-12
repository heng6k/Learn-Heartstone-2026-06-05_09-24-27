using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    [DisallowMultipleComponent]
    public sealed class UnityFocusTrap : MonoBehaviour
    {
        private GameObject previousSelection;
        private GameObject initialSelection;
        private EventSystem activeEventSystem;
        private bool active;

        public void Activate(GameObject initialFocus)
        {
            activeEventSystem = ResolveEventSystem();
            previousSelection = activeEventSystem != null ? activeEventSystem.currentSelectedGameObject : null;
            initialSelection = initialFocus;
            active = true;
            EnforceFocus();
        }

        public void EnforceFocus()
        {
            if (!active)
            {
                return;
            }

            activeEventSystem = activeEventSystem != null ? activeEventSystem : ResolveEventSystem();
            if (activeEventSystem == null)
            {
                return;
            }

            var selected = activeEventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
            {
                return;
            }

            var target = IsFocusable(initialSelection)
                ? initialSelection
                : GetComponentsInChildren<Selectable>(true)
                    .FirstOrDefault(selectable => selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
                    ?.gameObject;
            if (target != null)
            {
                activeEventSystem.SetSelectedGameObject(target);
            }
        }

        public void Release()
        {
            if (!active)
            {
                return;
            }

            active = false;
            if (activeEventSystem != null && previousSelection != null && previousSelection.activeInHierarchy)
            {
                activeEventSystem.SetSelectedGameObject(previousSelection);
            }

            previousSelection = null;
            initialSelection = null;
            activeEventSystem = null;
        }

        private void LateUpdate()
        {
            EnforceFocus();
        }

        private void OnDisable()
        {
            Release();
        }

        private static bool IsFocusable(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                return false;
            }

            var selectable = target.GetComponent<Selectable>();
            return selectable == null || selectable.IsInteractable();
        }

        private static EventSystem ResolveEventSystem()
        {
            return EventSystem.current != null
                ? EventSystem.current
                : FindAnyObjectByType<EventSystem>();
        }
    }
}
