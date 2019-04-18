﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ItemStorageSaveData
{
    public List<string> itemSlugs;

    public ItemStorageSaveData(ItemStorage itemStorage)
    {
        itemSlugs = new List<string>();

        foreach (Item item in itemStorage.storageItems)
            itemSlugs.Add(item.ItemSlug);
    }
}

[RequireComponent(typeof(Animator))]
public class ItemStorage : Interactable
{
    Animator animator;

    [Space]
    [SerializeField] CanvasGroup chestCanvasGroup;
    [SerializeField] Button closeButton;

    [Space]
    [SerializeField] RectTransform playerItemsHolder;
    [SerializeField] RectTransform storageItemsHolder;

    InventoryUIItem itemContainer { get; set; }

    [Space]
    public List<Item> playerItems = new List<Item>();
    public List<Item> storageItems = new List<Item>();

    protected override void Awake()
    {
        base.Awake();

        itemContainer = Resources.Load<InventoryUIItem>("UI/Item_Container");

        // Save/Load
        SaveLoadController.OnSaveGame += SaveLoadController_OnSaveGame;
        SaveLoadController.OnLoadGame += SaveLoadController_OnLoadGame;

        animator = GetComponent<Animator>();
        itemContainer = Resources.Load<InventoryUIItem>("UI/Item_Container");

        SetCanvasGroupActive(false);
    }

    public override void Interact()
    {
        if (HasInteracted)
            return;

        // Play the OpenChest animation, then display the chest UI
        animator.SetTrigger("OpenChest");
        StartCoroutine(OpenChestDelay(0.6f));
        UIEventHandler.UIDisplayed(true);
        UIEventHandler.ItemStorageActive(true);

        base.Interact();
    }

    
    public void ReceiveElement(GameObject obj, bool inStorage)
    {
        InventoryUIItem uiItem = obj.GetComponent<InventoryUIItem>();
        if (uiItem == null)
            return;
        
        if (inStorage)
        {
            print("Add [" + obj.name + "] to storage. Remove from inventory.");
            storageItems.Add(uiItem.Item);
            playerItems.Remove(uiItem.Item);
        }
        else
        {
            print("Add [" + obj.name + "] to inventory. Remove from storage.");
            playerItems.Add(uiItem.Item);
            storageItems.Remove(uiItem.Item);
        }
    }
    

    #region Open/Close Chest

    /// <summary>
    /// Displays the chest UI
    /// </summary>
    private void OpenChest()
    {
        SetCanvasGroupActive(true);
        closeButton.onClick.AddListener(delegate { CloseChest(); });

        // Get an updated list of Items in the player's inventory
        InventoryManager.OnItemListUpdated += InventoryManager_OnItemListUpdated;
        InventoryManager.instance.UpdatedItemList();
    }

    /// <summary>
    /// Hides the chest display. Closes the chest.
    /// </summary>
    private void CloseChest()
    {
        animator.SetTrigger("CloseChest");
        SetCanvasGroupActive(false);
        UIEventHandler.UIDisplayed(false);
        UIEventHandler.ItemStorageActive(false);

        InventoryManager.OnItemListUpdated -= InventoryManager_OnItemListUpdated;
        closeButton.onClick.RemoveAllListeners();
        HasInteracted = false;
    }

    #endregion

    #region Helper Functions
    
    private void AddItemUI(Item item, RectTransform parent)
    {
        InventoryUIItem emptyItem = Instantiate(itemContainer, parent);
        emptyItem.SetItem(item);
    }

    private void RemoveItemUI(Item item, RectTransform parent)
    {
        foreach (Transform t in parent)
        {
            if (t.GetComponent<InventoryUIItem>() == null)
                continue;

            if (t.GetComponent<InventoryUIItem>().Item.ItemSlug == item.ItemSlug)
            {
                Destroy(t.gameObject);
                break;
            }
        }
    }

    /// <summary>
    /// Hides or displays the Canvas via the CanvasGroup component.
    /// </summary>
    /// <param name="state"></param>
    private void SetCanvasGroupActive(bool state)
    {
        chestCanvasGroup.alpha = (state) ? 1f : 0f;
        chestCanvasGroup.interactable = state;
        chestCanvasGroup.blocksRaycasts = state;
    }

    private IEnumerator OpenChestDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        OpenChest();
    }

    #endregion

    #region Event Listeners
    
    private void InventoryManager_OnItemListUpdated(List<Item> updatedList)
    {
        playerItems = updatedList;

        // Remove all old UI elements
        foreach (Item item in storageItems)
            RemoveItemUI(item, playerItemsHolder);

        // Construct new list of UI Item elements
        foreach (Item item in updatedList)
            AddItemUI(item, playerItemsHolder);
    }

    private void SaveLoadController_OnSaveGame()
    {
        SaveSystem.SaveItemStorage(this, Application.persistentDataPath + "/itemstorage.dat");
    }

    private void SaveLoadController_OnLoadGame()
    {
        print("SaveLoadController_OnLoadGame()");

        ItemStorageSaveData data = SaveSystem.LoadData<ItemStorageSaveData>(Application.persistentDataPath + "/itemstorage.dat");

        if (data == null)
            return;

        // From load - Give saved Items
        foreach (string itemSlug in data.itemSlugs)
        {
            //if (!playerItems.Contains(GetItem(itemSlug)))
            storageItems.Add(ItemDatabase.instance.GetItem(itemSlug));
            print("Add: " + itemSlug);
        }

        foreach (Item item in storageItems)
            AddItemUI(item, storageItemsHolder);
    }

    #endregion
}
