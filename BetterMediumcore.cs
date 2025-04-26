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

        [Label("Delete Armor Pieces")]
        [DefaultValue(false)]
        public bool DeleteArmor { get; set; }

        [Label("Armor Deletion % (0-100)")]
        [Range(0, 100)]
        [DefaultValue(100)]
        public int ArmorDeletionPercent { get; set; }

        [Label("Armor Delete Count")]
        [DefaultValue(3)]
        public int ArmorDeleteCount { get; set; }

        [Label("Delete Trinkets")]
        [DefaultValue(false)]
        public bool DeleteTrinkets { get; set; }

        [Label("Trinket Deletion % (0-100)")]
        [Range(0, 100)]
        [DefaultValue(100)]
        public int TrinketDeletionPercent { get; set; }

        [Label("Trinket Delete Count")]
        [DefaultValue(7)]
        public int TrinketDeleteCount { get; set; }

        [Label("Delete Hotbar Items")]
        [DefaultValue(false)]
        public bool DeleteHotbar { get; set; }

        [Label("Hotbar Deletion % (0-100)")]
        [Range(0, 100)]
        [DefaultValue(100)]
        public int HotbarDeletionPercent { get; set; }

        [Label("Hotbar Delete Count")]
        [DefaultValue(10)]
        public int HotbarDeleteCount { get; set; }

        [Label("Delete Inventory Items")]
        [DefaultValue(false)]
        public bool DeleteInventory { get; set; }

        [Label("Inventory Deletion % (0-100)")]
        [Range(0, 100)]
        [DefaultValue(100)]
        public int InventoryDeletionPercent { get; set; }

        [Label("Inventory Delete Count")]
        [DefaultValue(40)]
        public int InventoryDeleteCount { get; set; }

        [Header("ItemModifierSettings")]
        [Label("Modifier Removal % (0-100)")]
        [Range(0, 100)]
        [DefaultValue(100)]
        public int ModifierRemovalPercent { get; set; }

        [Label("Number of Modifiers to Remove")]
        [DefaultValue(1)]
        public int ModifierRemovals { get; set; }
    }

    public class BetterMediumcore : Mod
    {
        public override void Load()
        {
            ModContent.GetInstance<BetterMediumcoreConfig>();
        }
    }

    public class NoModifiersPlayer : ModPlayer
    {
        private Random random = new Random();

        public override void OnRespawn()
        {
            var cfg = ModContent.GetInstance<BetterMediumcoreConfig>();
            List<string> messages = new List<string>();

            if (cfg.DeleteArmor)
                RollAndProcess("Armor", cfg.ArmorDeletionPercent,
                    () => DeleteRandomArmorPieces(cfg.ArmorDeleteCount), messages);

            if (cfg.DeleteTrinkets)
                RollAndProcess("Trinkets", cfg.TrinketDeletionPercent,
                    () => DeleteRandomTrinkets(cfg.TrinketDeleteCount), messages);

            if (cfg.DeleteHotbar)
                RollAndProcess("Hotbar", cfg.HotbarDeletionPercent,
                    () => DeleteRandomHotbarItems(cfg.HotbarDeleteCount), messages);

            if (cfg.DeleteInventory)
                RollAndProcess("Inventory", cfg.InventoryDeletionPercent,
                    () => DeleteRandomInventoryItems(cfg.InventoryDeleteCount), messages);

            if (cfg.ModifierRemovals > 0)
                RollAndProcess("Modifier Removal", cfg.ModifierRemovalPercent,
                    () => RemoveRandomItemModifiers(cfg.ModifierRemovals), messages);

            if (messages.Count > 0)
                ShowDeletionSummary(messages);
        }

        private void RollAndProcess(string category, int percentChance, Func<int> action, List<string> messages)
        {
            double roll = random.NextDouble() * 100;
            bool shouldDelete = roll <= percentChance;
            messages.Add($"[BetterMC] {category} roll: {roll:F2}% vs {percentChance}% => {(shouldDelete ? "DELETE" : "SKIP")}. ");
            if (shouldDelete)
            {
                int removed = action();
                messages.Add($"[BetterMC] {category}: removed {removed} item(s).");
            }
        }

        private int DeleteRandomArmorPieces(int count)
        {
            var slots = Enumerable.Range(0, 3).Where(i => Player.armor[i]?.stack > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                Player.armor[slots[idx]].TurnToAir();
                slots.RemoveAt(idx);
                removed++;
            }
            ForceRefreshAppearance();
            return removed;
        }

        private int DeleteRandomTrinkets(int count)
        {
            var slots = Enumerable.Range(3, Player.armor.Length - 3)
                .Where(i => Player.armor[i]?.stack > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                Player.armor[slots[idx]].TurnToAir();
                slots.RemoveAt(idx);
                removed++;
            }
            ForceRefreshAppearance();
            return removed;
        }

        private int DeleteRandomHotbarItems(int count)
        {
            var slots = Enumerable.Range(0, 10).Where(i => Player.inventory[i]?.stack > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                Player.inventory[slots[idx]].TurnToAir();
                slots.RemoveAt(idx);
                removed++;
            }
            return removed;
        }

        private int DeleteRandomInventoryItems(int count)
        {
            var slots = Enumerable.Range(10, Player.inventory.Length - 10)
                .Where(i => Player.inventory[i]?.stack > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                Player.inventory[slots[idx]].TurnToAir();
                slots.RemoveAt(idx);
                removed++;
            }
            return removed;
        }

        private int RemoveRandomItemModifiers(int count)
        {
            var items = Player.inventory.Concat(Player.armor)
                .Where(it => it != null && it.prefix > 0).ToList();
            int removed = 0;
            for (int i = 0; i < count && items.Count > 0; i++)
            {
                int idx = random.Next(items.Count);
                var item = items[idx];
                item.prefix = 0;
                item.SetDefaults(item.type);
                items.RemoveAt(idx);
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
