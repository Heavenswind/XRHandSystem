using UnityEngine;
using UnityEngine.InputSystem;
using XRHandSystem.Unity;

namespace XRHandSystem.Unity
{
    [RequireComponent(typeof(HandGrabber))]
    public class HandInputBinding : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pinchAction;
        [SerializeField] private float _grabThreshold = 0.8f;

        private HandGrabber _grabber;
        private bool _wasGrabbing;

        private void Awake()
        {
            _grabber = GetComponent<HandGrabber>();
        }

        private void OnEnable()  => _pinchAction?.action.Enable();
        private void OnDisable() => _pinchAction?.action.Disable();

        private void Update()
        {
            if (_pinchAction == null) return;

            float pinch      = _pinchAction.action.ReadValue<float>();
            bool  isGrabbing = pinch >= _grabThreshold;

            if (isGrabbing && !_wasGrabbing)
                _grabber.TryGrab();
            else if (!isGrabbing && _wasGrabbing)
                _grabber.Release();

            _wasGrabbing = isGrabbing;
        }
    }
}
