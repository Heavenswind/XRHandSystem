using UnityEngine;
using UnityEngine.InputSystem;

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

            // Pass raw pinch value to grabber so pose projection can use it
            _grabber.CurrentPinchValue = pinch;

            if (isGrabbing && !_wasGrabbing)
                _grabber.TryGrab();
            else if (!isGrabbing && _wasGrabbing)
                _grabber.Release();

            _wasGrabbing = isGrabbing;
        }
    }
}
