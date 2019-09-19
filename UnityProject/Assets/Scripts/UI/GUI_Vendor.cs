﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Random = UnityEngine.Random;

public class GUI_Vendor : NetTab
{
	[SerializeField]
	private bool allowSell = true;
	[SerializeField]
	private float cooldownTimer = 2f;

	private Vendor vendor;
	private List<VendorItem> vendorContent = new List<VendorItem>();
	[SerializeField]
	private EmptyItemList itemList;
	[SerializeField]
	private NetColorChanger hullColor;
	private bool inited = false;
	[SerializeField]
	private string vendMessage = "Item given.";
	[SerializeField]
	private string deniedMessage = "Bzzt.";
	[SerializeField]
	private string restockMessage = "Items restocked.";

	private void Start()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}
	}

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		vendor = Provider.GetComponent<Vendor>();
		hullColor.SetValue = ColorUtility.ToHtmlStringRGB(vendor.HullColor);
		inited = true;
		GenerateContentList();
		UpdateList();
		vendor.OnRestockUsed.AddListener(RestockItems);
	}

	private void GenerateContentList()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		vendorContent = new List<VendorItem>();
		for (int i = 0; i < vendor.VendorContent.Count; i++)
		{
			vendorContent.Add(new VendorItem(vendor.VendorContent[i]));
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (!CustomNetworkManager.Instance._isServer || !inited)
		{
			return;
		}
		UpdateList();
		allowSell = true;
	}

	public void RestockItems()
	{
		GenerateContentList();
		UpdateList();
		SendToChat(restockMessage);
	}

	private void UpdateList()
	{
		itemList.Clear();
		itemList.AddItems(vendorContent.Count);
		for (int i = 0; i < vendorContent.Count; i++)
		{
			VendorItemEntry item = itemList.Entries[i] as VendorItemEntry;
			item.SetItem(vendorContent[i], this);
		}
	}

	public void VendItem(VendorItem item)
	{
		VendorItem itemToSpawn = null;
		foreach (var vendorItem in vendorContent)
		{
			if (vendorItem == item)
			{
				itemToSpawn = item;
				break;
			}
		}

		if (!CanSell(itemToSpawn))
		{
			return;
		}

		Vector3 spawnPos = vendor.gameObject.RegisterTile().WorldPositionServer;
		var spawnedItem = PoolManager.PoolNetworkInstantiate(itemToSpawn.Item, spawnPos, vendor.transform.parent);
		itemToSpawn.Stock--;
		PostToChatMessage.Send("Item given", ChatChannel.Examine);

		//Ejecting in direction
		if (vendor.EjectObjects && vendor.EjectDirection != EjectDirection.None)
		{
			Vector3 offset = Vector3.zero;
			switch (vendor.EjectDirection)
			{
				case EjectDirection.Up:
					offset = vendor.transform.rotation * Vector3.up / Random.Range(4, 12);
					break;
				case EjectDirection.Down:
					offset = vendor.transform.rotation * Vector3.down / Random.Range(4, 12);
					break;
				case EjectDirection.Random:
					offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0);
					break;
			}
			spawnedItem.GetComponent<CustomNetTransform>()?.Throw(new ThrowInfo
			{
				ThrownBy = gameObject,
				Aim = BodyPartType.Chest,
				OriginPos = spawnPos,
				TargetPos = spawnPos + offset,
				SpinMode = (vendor.EjectDirection == EjectDirection.Random) ? SpinMode.Clockwise : SpinMode.None
			});
		}

		SendToChat(vendMessage);
		UpdateList();
		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	private bool CanSell(VendorItem itemToSpawn)
	{
		if (allowSell && itemToSpawn != null && itemToSpawn.Stock > 0)
		{
			return true;
		}
		SendToChat(deniedMessage);
		return false;
	}

	private void SendToChat(string messageToSend)
	{
		ChatEvent chatEvent = new ChatEvent();

		chatEvent.speaker = vendor.name;
		chatEvent.channels = ChatChannel.Local;
		chatEvent.message = messageToSend;
		chatEvent.position = vendor.transform.position;
		chatEvent.radius = 3f;
		ChatRelay.Instance.AddToChatLogServer(chatEvent);
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return WaitFor.Seconds(cooldownTimer);
		allowSell = true;
	}
}
