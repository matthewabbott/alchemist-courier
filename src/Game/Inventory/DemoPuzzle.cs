using System.Collections.Generic;
using AlchemistCourier.Core.Grid;
using AlchemistCourier.Core.Bags;
using AlchemistCourier.Core.Reagents;
using AlchemistCourier.Core.Chemistry;

namespace AlchemistCourier.Game.Inventory;

public static class DemoPuzzle
{
    public static (MetaGrid grid, List<Reagent> trayReagents) Create()
    {
        var grid = new MetaGrid(8, 8);

        // Place two bags
        var pouch = new Bag(BagDefinition.SmallPouch); // 3x3 at (1,1)
        grid.PlaceBag(pouch, new Vector2I(1, 1));

        // Reinforced satchel allows Sharp reagents on edge cells
        var satchelDef = new BagDefinition("satchel", "Satchel", new bool[,]
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
        }, new[] { BagPropertyType.Reinforced });
        var satchel = new Bag(satchelDef); // 4x3 Reinforced at (4,1)
        grid.PlaceBag(satchel, new Vector2I(4, 1));

        // Create reagents with varied aspects for color variety
        var firePebble = new Reagent(new ReagentDefinition(
            "fire_pebble", "Fire Pebble",
            new bool[,] { { true } },
            aspectsPerCell: new[] { new AspectSet(Aspect.Hot(2)) }
        ));

        var iceShard = new Reagent(new ReagentDefinition(
            "ice_shard", "Ice Shard",
            new bool[,] { { true, true } },
            aspectsPerCell: new[] { new AspectSet(Aspect.Cold()), new AspectSet(Aspect.Cold()) }
        ));

        var poisonFang = new Reagent(new ReagentDefinition(
            "poison_fang", "Poison Fang",
            new bool[,]
            {
                { true, false },
                { true, false },
                { true, true  },
            },
            aspectsPerCell: new[]
            {
                new AspectSet(Aspect.Toxic()),
                new AspectSet(Aspect.Toxic()),
                new AspectSet(Aspect.Toxic()),
                new AspectSet(Aspect.Sharp()),
            }
        ));

        var blastJelly = new Reagent(new ReagentDefinition(
            "blast_jelly", "Blast Jelly",
            new bool[,]
            {
                { true, true, true },
                { false, true, false },
            },
            aspectsPerCell: new[]
            {
                new AspectSet(Aspect.Volatile()),
                new AspectSet(Aspect.Hot()),
                new AspectSet(Aspect.Volatile()),
                new AspectSet(Aspect.Volatile()),
            }
        ));

        var inertCrystal = new Reagent(new ReagentDefinition(
            "inert_crystal", "Inert Crystal",
            new bool[,] { { true, true } },
            aspectsPerCell: new[] { new AspectSet(Aspect.Stable()), new AspectSet(Aspect.Stable()) }
        ));

        var cinnabarBlock = new Reagent(new ReagentDefinition(
            "cinnabar_block", "Cinnabar Block",
            new bool[,]
            {
                { true, true },
                { true, true },
            },
            rarity: Rarity.Rare,
            aspectsPerCell: new[]
            {
                new AspectSet(Aspect.Heavy()),
                new AspectSet(Aspect.Medicinal()),
                new AspectSet(Aspect.Catalytic()),
                new AspectSet(Aspect.Heavy()),
            }
        ));

        return (grid, new List<Reagent>
        {
            firePebble, iceShard, poisonFang, blastJelly, inertCrystal, cinnabarBlock
        });
    }
}
