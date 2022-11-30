using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HapticButtonBehaviour : MonoBehaviour, IPointerEnterHandler {
  void Start() {}

  void Update() {}

  public void OnPointerEnter(PointerEventData eventData) {
    var controller = GameObject.Find("/ObjectManipulator")
                         .GetComponent<ObjectManipulator>()
                         .Controller;
    StartCoroutine(ObjectManipulator.ControllerPulse(0.2f, 0.01f, controller));
  }
}
