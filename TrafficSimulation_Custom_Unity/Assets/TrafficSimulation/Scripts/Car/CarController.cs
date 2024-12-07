using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [SerializeField] private Car _car;
    private Controls _controls;
    
    private void Awake()
    {
        _controls = new Controls();
        _controls.Enable();

        _controls.Car.HandBreak.performed += OnHandBreakInput;
        _controls.Car.HandBreak.canceled += OnHandBreakInput;
    }
    
    private void Update()
    {
        _car.SteerWheelInput = _controls.Car.Steer.ReadValue<float>();
        _car.ThrottleInput = _controls.Car.Throttle.ReadValue<float>();
        _car.BreakInput = _controls.Car.Break.ReadValue<float>();
    }
    
    private void OnHandBreakInput(InputAction.CallbackContext context)
    {
        _car.IsHandBrakeEngaged = context.ReadValueAsButton();
    }
}
