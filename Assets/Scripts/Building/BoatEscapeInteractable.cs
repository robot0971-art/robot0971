using UnityEngine;
using SunnysideIsland.Core;
using SunnysideIsland.UI.Menu;

namespace SunnysideIsland.Building
{
    public sealed class BoatEscapeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Building _building;

        private void Awake()
        {
            if (_building == null)
            {
                _building = GetComponent<Building>();
            }
        }

        public void Interact()
        {
            if (!CanInteract())
            {
                Debug.Log("[BoatEscapeInteractable] Interact blocked: boat not completed");
                return;
            }

            Debug.Log("[BoatEscapeInteractable] Interact -> opening BoatConfirmPanel");
            var panel = FindFirstObjectByType<BoatConfirmPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("[BoatEscapeInteractable] BoatConfirmPanel not found.");
                return;
            }

            panel.Show(ConfirmEscape);
        }

        public bool CanInteract()
        {
            return _building != null && _building.State == BuildingState.Completed;
        }

        public string GetInteractionHint()
        {
            return CanInteract()
                ? "E: 탈출"
                : "배가 완성되어야 한다";
        }

        private void ConfirmEscape()
        {
            Debug.Log("[BoatEscapeInteractable] ConfirmEscape()");
            GameManager.Instance?.OnBoatBuilt();
        }
    }
}
