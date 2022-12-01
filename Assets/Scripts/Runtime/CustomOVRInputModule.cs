using UnityEngine;
using UnityEngine.EventSystems;

public class CustomOVRInputModule : OVRInputModule {
  private OVRInput.Controller _dominantHand;

  protected override void Start() {
    ObjectManipulator.DominantHandChanged += controller => _dominantHand =
        controller;

    base.Start();
  }

  protected override MouseState GetGazePointerData() {
    var mouseState = base.GetGazePointerData();
    var leftData = mouseState.GetButtonState(PointerEventData.InputButton.Left)
                       .eventData.buttonData;
    leftData.scrollDelta = GetDominantHandScrollDelta();
    var rightData =
        mouseState.GetButtonState(PointerEventData.InputButton.Right)
            .eventData.buttonData;
    rightData.scrollDelta = GetNonDominantHandScrollDelta();
    return mouseState;
  }

  private Vector2 GetDominantHandScrollDelta() {
    var scrollDelta =
        _dominantHand switch { OVRInput.Controller.LTouch => OVRInput.Get(
                                   OVRInput.Axis2D.PrimaryThumbstick),
                               OVRInput.Controller.RTouch => OVRInput.Get(
                                   OVRInput.Axis2D.SecondaryThumbstick),
                               _ => new Vector2() };

    if (Mathf.Abs(scrollDelta.x) < rightStickDeadZone)
      scrollDelta.x = 0;
    if (Mathf.Abs(scrollDelta.y) < rightStickDeadZone)
      scrollDelta.y = 0;

    return scrollDelta;
  }

  private Vector2 GetNonDominantHandScrollDelta() {
    var scrollDelta =
        _dominantHand switch { OVRInput.Controller.LTouch => OVRInput.Get(
                                   OVRInput.Axis2D.SecondaryThumbstick),
                               OVRInput.Controller.RTouch => OVRInput.Get(
                                   OVRInput.Axis2D.PrimaryThumbstick),
                               _ => new Vector2() };

    if (Mathf.Abs(scrollDelta.x) < rightStickDeadZone)
      scrollDelta.x = 0;
    if (Mathf.Abs(scrollDelta.y) < rightStickDeadZone)
      scrollDelta.y = 0;

    return scrollDelta;
  }
}
