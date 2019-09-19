﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class APCPoweredDevice : NetworkBehaviour
{
	public float MinimumWorkingVoltage = 190;
	public float MaximumWorkingVoltage = 300;
	public bool IsEnvironmentalDevice = false;
	public float Wattusage = 0.01f;
	public float Resistance = 99999999;
	public APC RelatedAPC;
	public IAPCPowered Powered;
	public bool AdvancedControlToScript;

	[SyncVar(hook = nameof(UpdateSynchronisedState))]
	public PowerStates State;

	public override void OnStartClient()
	{
		UpdateSynchronisedState(State);
	}

	public override void OnStartServer()
	{
		UpdateSynchronisedState(State);
	}

	private void Awake()
	{
		Powered = gameObject.GetComponent<IAPCPowered>();
	}

	void Start()
	{
		Logger.LogTraceFormat("{0}({1}) starting, state {2}", Category.Electrical, name, transform.position.To2Int(), State);
		if (Wattusage > 0)
		{
			Resistance = 240 / (Wattusage / 240);
		}
		if (RelatedAPC != null)
		{
			if (IsEnvironmentalDevice)
			{
				RelatedAPC.EnvironmentalDevices.Add(this);
			}
			else {
				RelatedAPC.ConnectedDevices.Add(this);
			}
		}
	}
	public void APCBroadcastToDevice(APC APC)
	{
		if (RelatedAPC == null)
		{
			RelatedAPC = APC;
			if (IsEnvironmentalDevice)
			{
				RelatedAPC.EnvironmentalDevices.Add(this);
			}
			else {
				RelatedAPC.ConnectedDevices.Add(this);
			}
		}
	}
	public void PowerNetworkUpdate(float Voltage) //Could be optimised to not update when voltage is same as previous voltage
	{
		if (Powered != null)
		{
			if (AdvancedControlToScript)
			{
				Powered.PowerNetworkUpdate(Voltage);
			}
			else {
				if (Voltage <= 1)
				{
					State = PowerStates.Off;
				}
				else if (Voltage > MaximumWorkingVoltage)
				{
					State = PowerStates.OverVoltage;
				}
				else if (Voltage < MinimumWorkingVoltage)
				{
					State = PowerStates.LowVoltage;
				}
				else {
					State = PowerStates.On;
				}
				Powered.StateUpdate(State);
			}

		}
	}
	public void OnDisable()
	{
		if (RelatedAPC != null)
		{
			if (IsEnvironmentalDevice)
			{
				if (RelatedAPC.EnvironmentalDevices.Contains(this))
				{
					RelatedAPC.EnvironmentalDevices.Remove(this);
				}

			}
			else {
				if (RelatedAPC.ConnectedDevices.Contains(this))
				{
					RelatedAPC.ConnectedDevices.Remove(this);
				}
			}
		}
	}
	private void UpdateSynchronisedState(PowerStates _State) {
		if (_State != State)
		{
			Logger.LogTraceFormat("{0}({1}) state changing {2} to {3}", Category.Electrical, name, transform.position.To2Int(), State, _State);
		}

		State = _State;
		if (Powered != null)
		{
			Powered.StateUpdate(State);
		}
	}
}


public enum PowerStates{
	Off,
	LowVoltage,
	On,
	OverVoltage,
}