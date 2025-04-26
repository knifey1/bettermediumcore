using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace BetterMediumcore
{
    public class BetterMediumcoreConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(false)]
        public bool DropItems { get; set; }

        [DefaultValue(false)]
        public bool DeleteArmor { get; set; }
        [Range(0, 100)]
        [DefaultValue(100)]
        public int ArmorDeletionPercent { get; set; }
        [DefaultValue(3)]
        public int ArmorDeleteCount { get; set; }

        [DefaultValue(false)]
        public bool DeleteTrinkets { get; set; }
        [Range(0, 100)]
        [DefaultValue(100)]
        public int TrinketDeletionPercent { get; set; }
        [DefaultValue(7)]
        public int TrinketDeleteCount { get; set; }

        [DefaultValue(false)]
        public bool DeleteHotbar { get; set; }
        [Range(0, 100)]
        [DefaultValue(100)]
        public int HotbarDeletionPercent { get; set; }
        [DefaultValue(10)]
        public int HotbarDeleteCount { get; set; }

        [DefaultValue(false)]
        public bool DeleteInventory { get; set; }
        [Range(0, 100)]
        [DefaultValue(100)]
        public int InventoryDeletionPercent { get; set; }
        [DefaultValue(40)]
        public int InventoryDeleteCount { get; set; }

        [DefaultValue(true)]
        public bool RemoveModifiers { get; set; }
        [Range(0, 100)]
        [DefaultValue(100)]
        public int ModifierRemovalPercent { get; set; }
        [DefaultValue(1)]
        public int ModifierRemovals { get; set; }

        [DefaultValue(true)]
        public bool ShowMessages { get; set; }
    }

    public class BetterMediumcore : Mod
    {
        public override void Load() => ModContent.GetInstance<BetterMediumcoreConfig>();
    }

    public class NoModifiersPlayer : ModPlayer
    {
        private Random random = new Random();

        public override void OnRespawn()
        {
            var cfg = ModContent.GetInstance<BetterMediumcoreConfig>();
            var messages = new List<string>();

            if (cfg.DeleteArmor)
                RollAndProcess("Armor", cfg.ArmorDeletionPercent,
                    () => HandleSlots(Enumerable.Range(0, 3), cfg.ArmorDeleteCount, cfg.DropItems, true), messages);

            if (cfg.DeleteTrinkets)
                RollAndProcess("Trinkets", cfg.TrinketDeletionPercent,
                    () => HandleSlots(Enumerable.Range(3, Player.armor.Length - 3), cfg.TrinketDeleteCount, cfg.DropItems, true), messages);

            if (cfg.DeleteHotbar)
                RollAndProcess("Hotbar", cfg.HotbarDeletionPercent,
                    () => HandleSlots(Enumerable.Range(0, 10), cfg.HotbarDeleteCount, cfg.DropItems, false), messages);

            if (cfg.DeleteInventory)
                RollAndProcess("Inventory", cfg.InventoryDeletionPercent,
                    () => HandleSlots(Enumerable.Range(10, Player.inventory.Length - 10), cfg.InventoryDeleteCount, cfg.DropItems, false), messages);

            if (cfg.RemoveModifiers)
                RollAndProcess("Modifier Removal", cfg.ModifierRemovalPercent,
                    () => RemoveRandomItemModifiers(cfg.ModifierRemovals), messages);

            if (cfg.ShowMessages && messages.Any())
                ShowDeletionSummary(messages);
        }

        private void RollAndProcess(string category, int percentChance, Func<int> action, List<string> messages)
        {
            double roll = random.NextDouble() * 100;
            bool shouldAct = roll <= percentChance;
            messages.Add($"[BetterMC] {category} roll: {roll:F2}% vs {percentChance}% => {(shouldAct ? "FAIL" : "PASS")}. ");
            if (shouldAct)
            {
                int count = action();
                messages.Add($"[BetterMC] {category}: processed {count} item(s).");
            }
        }

        private int HandleSlots(IEnumerable<int> indices, int count, bool dropItems, bool isArmorSlot)
        {
            var slots = indices.Where(i => (isArmorSlot ? Player.armor[i] : Player.inventory[i])?.stack > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                int slot = slots[idx];
                var item = isArmorSlot ? Player.armor[slot] : Player.inventory[slot];

                if (dropItems && item.stack > 0)
                    Item.NewItem(Player.GetSource_DropAsItem(), Player.position, Player.width, Player.height, item.type, item.stack);

                item.TurnToAir();
                slots.RemoveAt(idx);
                removed++;
            }
            if (isArmorSlot) ForceRefreshAppearance();
            return removed;
        }

        private int RemoveRandomItemModifiers(int count)
        {
            var items = Player.inventory.Concat(Player.armor).Where(it => it != null && it.prefix > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && items.Count > 0; i++)
            {
                var item = items[random.Next(items.Count)];
                item.prefix = 0;
                item.SetDefaults(item.type);
                items.Remove(item);
                removed++;
            }
            return removed;
        }

        private void ShowDeletionSummary(List<string> messages)
        {
            foreach (var msg in messages)
                Main.NewText(msg, Color.LightGreen);
        }

        private void ForceRefreshAppearance()
        {
            Player.head = Player.armor[0]?.headSlot ?? 0;
            Player.body = Player.armor[1]?.bodySlot ?? 0;
            Player.legs = Player.armor[2]?.legSlot ?? 0;
            Player.invis = !Player.invis;
            Player.invis = !Player.invis;
        }
    }
}
