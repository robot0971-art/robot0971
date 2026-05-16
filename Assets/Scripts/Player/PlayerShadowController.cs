using UnityEngine;

namespace SunnysideIsland.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerShadowController : MonoBehaviour
    {
        [SerializeField] private GameObject _shadowObject;

        private PlayerMovement _movement;
        private bool? _lastVisibleState;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();

            if (_shadowObject == null)
            {
                _shadowObject = FindChildByName("Shadow");
            }

            ApplyVisibility();
        }

        private void LateUpdate()
        {
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_shadowObject == null || _movement == null)
            {
                return;
            }

            bool shouldShowShadow = !_movement.IsSwimming;
            if (_lastVisibleState == shouldShowShadow)
            {
                return;
            }

            _shadowObject.SetActive(shouldShowShadow);
            _lastVisibleState = shouldShowShadow;
        }

        private GameObject FindChildByName(string childName)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (string.Equals(child.name, childName, System.StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
