using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewValley.Menus;

public class QuestContainerMenu : MenuWithInventory
{
	public enum ChangeType
	{
		None,
		Place,
		Grab
	}

	public InventoryMenu ItemsToGrabMenu;

	public Func<Item, int> stackCapacityCheck;

	public Action onItemChanged;

	public Action onConfirm;

	public QuestContainerMenu(IList<Item> inventory, int rows = 3, InventoryMenu.highlightThisItem highlight_method = null, Func<Item, int> stack_capacity_check = null, Action on_item_changed = null, Action on_confirm = null)
		: base(highlight_method, okButton: true)
	{
		this.onItemChanged = (Action)Delegate.Combine(this.onItemChanged, on_item_changed);
		this.onConfirm = (Action)Delegate.Combine(this.onConfirm, on_confirm);
		int capacity = inventory.Count;
		int containerWidth = 64 * (capacity / rows);
		this.ItemsToGrabMenu = new InventoryMenu(Game1.uiViewport.Width / 2 - containerWidth / 2, base.yPositionOnScreen + 64, playerInventory: false, inventory, null, capacity, rows);
		this.stackCapacityCheck = stack_capacity_check;
		for (int j = 0; j < this.ItemsToGrabMenu.actualInventory.Count; j++)
		{
			if (j >= this.ItemsToGrabMenu.actualInventory.Count - this.ItemsToGrabMenu.capacity / this.ItemsToGrabMenu.rows)
			{
				this.ItemsToGrabMenu.inventory[j].downNeighborID = j + 53910;
			}
		}
		for (int i = 0; i < base.inventory.inventory.Count; i++)
		{
			base.inventory.inventory[i].myID = i + 53910;
			if (base.inventory.inventory[i].downNeighborID != -1)
			{
				base.inventory.inventory[i].downNeighborID += 53910;
			}
			if (base.inventory.inventory[i].rightNeighborID != -1)
			{
				base.inventory.inventory[i].rightNeighborID += 53910;
			}
			if (base.inventory.inventory[i].leftNeighborID != -1)
			{
				base.inventory.inventory[i].leftNeighborID += 53910;
			}
			if (base.inventory.inventory[i].upNeighborID != -1)
			{
				base.inventory.inventory[i].upNeighborID += 53910;
			}
			if (i < 12)
			{
				base.inventory.inventory[i].upNeighborID = this.ItemsToGrabMenu.actualInventory.Count - this.ItemsToGrabMenu.capacity / this.ItemsToGrabMenu.rows;
			}
			foreach (ClickableComponent item in base.inventory.GetBorder(InventoryMenu.BorderSide.Right))
			{
				item.rightNeighborID = base.okButton.myID;
			}
		}
		base.dropItemInvisibleButton.myID = -500;
		this.ItemsToGrabMenu.dropItemInvisibleButton.myID = -500;
		this.populateClickableComponentList();
		if (Game1.options.SnappyMenus)
		{
			this.setCurrentlySnappedComponentTo(53910);
			this.snapCursorToCurrentSnappedComponent();
		}
	}

	public virtual int GetDonatableAmount(Item item)
	{
		if (item == null)
		{
			return 0;
		}
		int stack_capacity = item.Stack;
		if (this.stackCapacityCheck != null)
		{
			stack_capacity = Math.Min(stack_capacity, this.stackCapacityCheck(item));
		}
		return stack_capacity;
	}

	public virtual Item TryToGrab(Item item, int amount)
	{
		int grabbed_amount = Math.Min(amount, item.Stack);
		if (grabbed_amount == 0)
		{
			return item;
		}
		Item taken_stack = item.getOne();
		taken_stack.Stack = grabbed_amount;
		item.Stack -= grabbed_amount;
		InventoryMenu.highlightThisItem highlight_method = base.inventory.highlightMethod;
		base.inventory.highlightMethod = InventoryMenu.highlightAllItems;
		Item leftover_items = base.inventory.tryToAddItem(taken_stack);
		base.inventory.highlightMethod = highlight_method;
		if (leftover_items != null)
		{
			item.Stack += leftover_items.Stack;
		}
		this.onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	public virtual Item TryToPlace(Item item, int amount)
	{
		int stack_capacity = Math.Min(amount, this.GetDonatableAmount(item));
		if (stack_capacity == 0)
		{
			return item;
		}
		Item donation_stack = item.getOne();
		donation_stack.Stack = stack_capacity;
		item.Stack -= stack_capacity;
		Item leftover_items = this.ItemsToGrabMenu.tryToAddItem(donation_stack, "Ship");
		if (leftover_items != null)
		{
			item.Stack += leftover_items.Stack;
		}
		this.onItemChanged?.Invoke();
		if (item.Stack <= 0)
		{
			return null;
		}
		return item;
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item clicked_item2 = base.inventory.getItemAt(x, y);
			if (clicked_item2 != null)
			{
				int clicked_index2 = base.inventory.getInventoryPositionOfClick(x, y);
				base.inventory.actualInventory[clicked_index2] = this.TryToPlace(clicked_item2, clicked_item2.Stack);
			}
		}
		if (this.ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item clicked_item = this.ItemsToGrabMenu.getItemAt(x, y);
			if (clicked_item != null)
			{
				int clicked_index = this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				this.ItemsToGrabMenu.actualInventory[clicked_index] = this.TryToGrab(clicked_item, clicked_item.Stack);
			}
		}
		if (base.okButton.containsPoint(x, y) && this.readyToClose())
		{
			base.exitThisMenu();
		}
	}

	public override void receiveRightClick(int x, int y, bool playSound = true)
	{
		if (base.isWithinBounds(x, y))
		{
			Item clicked_item2 = base.inventory.getItemAt(x, y);
			if (clicked_item2 != null)
			{
				int clicked_index2 = base.inventory.getInventoryPositionOfClick(x, y);
				base.inventory.actualInventory[clicked_index2] = this.TryToPlace(clicked_item2, 1);
			}
		}
		if (this.ItemsToGrabMenu.isWithinBounds(x, y))
		{
			Item clicked_item = this.ItemsToGrabMenu.getItemAt(x, y);
			if (clicked_item != null)
			{
				int clicked_index = this.ItemsToGrabMenu.getInventoryPositionOfClick(x, y);
				this.ItemsToGrabMenu.actualInventory[clicked_index] = this.TryToGrab(clicked_item, 1);
			}
		}
	}

	protected override void cleanupBeforeExit()
	{
		this.onConfirm?.Invoke();
		base.cleanupBeforeExit();
	}

	public override void performHoverAction(int x, int y)
	{
		base.performHoverAction(x, y);
		this.ItemsToGrabMenu.hover(x, y, base.heldItem);
	}

	public override void draw(SpriteBatch b)
	{
		b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
		base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);
		Game1.drawDialogueBox(this.ItemsToGrabMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder, this.ItemsToGrabMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder, this.ItemsToGrabMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2, this.ItemsToGrabMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2, speaker: false, drawOnlyBox: true);
		this.ItemsToGrabMenu.draw(b);
		if (!base.hoverText.Equals(""))
		{
			IClickableMenu.drawHoverText(b, base.hoverText, Game1.smallFont);
		}
		base.heldItem?.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 16, Game1.getOldMouseY() + 16), 1f);
		base.drawMouse(b);
		string text = this.ItemsToGrabMenu.descriptionTitle;
		if (text != null && text.Length > 1)
		{
			IClickableMenu.drawHoverText(b, this.ItemsToGrabMenu.descriptionTitle, Game1.smallFont, 32 + ((base.heldItem != null) ? 16 : (-21)), 32 + ((base.heldItem != null) ? 16 : (-21)));
		}
	}
}
