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
            var summary = new List<string>();

            summary.AddRange(ProcessCategory(
                "Armor", 0, 3,
                cfg.DeleteArmor, cfg.ArmorDeletionPercent, cfg.ArmorDeleteCount,
                cfg.DropItems, true));

            summary.AddRange(ProcessCategory(
                "Trinket", 3, Player.armor.Length - 3,
                cfg.DeleteTrinkets, cfg.TrinketDeletionPercent, cfg.TrinketDeleteCount,
                cfg.DropItems, true));

            summary.AddRange(ProcessCategory(
                "Hotbar item", 0, 10,
                cfg.DeleteHotbar, cfg.HotbarDeletionPercent, cfg.HotbarDeleteCount,
                cfg.DropItems, false));

            summary.AddRange(ProcessCategory(
                "Inventory item", 10, Player.inventory.Length - 10,
                cfg.DeleteInventory, cfg.InventoryDeletionPercent, cfg.InventoryDeleteCount,
                cfg.DropItems, false));

            if (cfg.RemoveModifiers)
            {
                var mods = RemoveModifiersList(cfg.ModifierRemovalPercent, cfg.ModifierRemovals);
                if (mods.Any())
                    summary.Add("Modifiers stripped from: " + string.Join(", ", mods));
            }

            if (cfg.ShowMessages && summary.Any())
                ShowSummary(summary);
        }

        private IEnumerable<string> ProcessCategory(
            string label,
            int startIndex,
            int slotCount,
            bool enabled,
            int chance,
            int removeCount,
            bool drop,
            bool isArmorSlot)
        {
            if (!enabled)
                yield break;

            // 1) Roll and report:
            double roll = random.NextDouble() * 100;
            yield return $"{label} deletion roll: {roll:F2}% (needs ≤ {chance}%)";

            // 2) If roll fails, report and exit:
            if (roll > chance)
            {
                yield return $"No {label} deleted.";
                yield break;
            }

            // 3) Perform deletions:
            var slots = Enumerable.Range(startIndex, slotCount)
                .Where(i => (isArmorSlot ? Player.armor[i] : Player.inventory[i])?.stack > 0)
                .ToList();

            for (int i = 0; i < removeCount && slots.Count > 0; i++)
            {
                int idx = random.Next(slots.Count);
                int slot = slots[idx];
                var item = isArmorSlot ? Player.armor[slot] : Player.inventory[slot];
                string name = $"{item.Name} x{item.stack}";

                if (drop)
                    Item.NewItem(
                        Player.GetSource_DropAsItem(),
                        Player.position, Player.width, Player.height,
                        item.type, item.stack);

                item.TurnToAir();

                if (isArmorSlot)
                {
                    // Dye slot
                    if (slot < Player.dye.Length && Player.dye[slot]?.stack > 0)
                    {
                        var dye = Player.dye[slot];
                        if (drop)
                            Item.NewItem(
                                Player.GetSource_DropAsItem(),
                                Player.position, Player.width, Player.height,
                                dye.type, dye.stack);
                        dye.TurnToAir();
                    }
                    // Cosmetic armor slot
                    if (slot + 10 < Player.armor.Length && Player.armor[slot + 10]?.stack > 0)
                    {
                        var cosmetic = Player.armor[slot + 10];
                        if (drop)
                            Item.NewItem(
                                Player.GetSource_DropAsItem(),
                                Player.position, Player.width, Player.height,
                                cosmetic.type, cosmetic.stack);
                        cosmetic.TurnToAir();
                    }
                }

                slots.RemoveAt(idx);
                yield return $"You lost {label}: {name}";
            }

            // 4) Refresh appearance if armor changed:
            if (isArmorSlot)
                ForceRefreshAppearance();
        }

        private List<string> RemoveModifiersList(int chance, int count)
        {
            if (random.NextDouble() * 100 > chance)
                return new List<string>();

            var itemsWithPrefix = Player.inventory
                .Concat(Player.armor)
                .Where(it => it != null && it.prefix > 0)
                .ToList();

            var names = new List<string>();
            for (int i = 0; i < count && itemsWithPrefix.Count > 0; i++)
            {
                var item = itemsWithPrefix[random.Next(itemsWithPrefix.Count)];
                names.Add(item.Name);
                item.prefix = 0;
                item.SetDefaults(item.type);
                itemsWithPrefix.Remove(item);
            }
            return names;
        }

        private void ShowSummary(List<string> summary)
        {
            Main.NewText("On death, you lost:", Color.LightGreen);
            foreach (var line in summary)
                Main.NewText("- " + line, Color.LightGreen);
        }

        private void ForceRefreshAppearance()
        {
            Player.head  = Player.armor[0]?.headSlot ?? 0;
            Player.body  = Player.armor[1]?.bodySlot ?? 0;
            Player.legs  = Player.armor[2]?.legSlot  ?? 0;
            Player.invis = !Player.invis;
            Player.invis = !Player.invis;
        }
    }
}
