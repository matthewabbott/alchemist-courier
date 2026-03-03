#  Alchemist Courier

# DESIGN DOCUMENT

---

[**Introduction**](#heading)	**[2](#heading)**

[Game Summary](#game-summary-pitch)	[2](#game-summary-pitch)

[Inspiration](#inspiration)	[2](#inspiration)

[Player Experience](#player-experience)	[2](#player-experience)

[Platform](#platform)	[3](#platform)

[Development Software](#development-software)	[3](#development-software)

[Genre](#genre)	[3](#genre)

[Target Audience](#target-audience)	[3](#target-audience)

[**Concept**](#concept)	**[3](#concept)**

[Gameplay overview](#gameplay-overview)	[3](#gameplay-overview)

[Primary Mechanics](#primary-mechanics)	[4](#primary-mechanics)

[Secondary Mechanics](#aspirational-mechanics)	[5](#aspirational-mechanics)

[**Art**](#art)	**[6](#art)**

[Design](#design)	[6](#design)

[**Audio**](#audio)	**[7](#audio)**

[Music](#music)	[7](#music)

[Sound Effects](#sound-effects)	[7](#sound-effects)

[**Game Experience**](#game-experience)	**[7](#game-experience)**

[UI](#ui)	[7](#ui)

[Controls](#controls)	[7](#controls)

[**Development Timeline**](#development-timeline)	**[8](#development-timeline)**

## **![][image1]** {#heading}

## **Introduction**

### **Game Summary Pitch** {#game-summary-pitch}

	Alchemist courier is roguelite/progressive puzzle game about playing inventory tetris with items that have various restrictions.

### **Inspiration** {#inspiration}

**Backpack Hero**  
Backpack Hero is a roguelike in which the hero fights enemies in autobattler fashion using the items in their backpack. The backpack is an inventory grid, and the items interact with one another and the enemies according to their arrangement. Much like other roguelikes, the player doesn't really choose what items they get, but they assemble a build based on what they find, trying to create the strongest autobattler loadout by organizing the items in their inventory in the right way.

**Resident Evil 4('s inventory system)**  
Resident Evil 4 and many other games like it have a grid based inventory system where some items take up more slots. That's the entire conceit of this game.

**Recettear('s dungeon exploration)**
While Recettear is about managign a shop, it also has a gameplay loop in which the main character dispatches adventurers to do simple dungeoneering in order to gain rare resources. Spiritually, this would be similar.

**(Aspirationally) Pokemon Mystery Dungeon**
While Pokemon Mystery Dungeon has dungeons that are pure roguelike, most of the game expects the player to accumulate character levels and items to bring with them into the dungeon. Specifically, they can intentionally bring items with them as preparation. There is less of a notion of 'starting over from 0' or 'permadeath'. They usually have a base they can customize at home as well. Furthermore, dungeons have the notion of 'hunger', which limits how much time you can spend in them.

### **Player Experience** {#player-experience}

In a series of slay-the-spire like encounters, the player will gather items (reagents) which will all have a suite of requirements on them (inventory shape, weight, and alchemical aspects (volatile, fragile, albedinous, firey, etc.)) and bring them home, managing their inventory and their character's ability to venture further (and return). They then are rewarded with incremental permanent progression resources.

### **Platform** {#platform}

The game is developed to be released on windows PC  
	

### **Development Software** {#development-software}

- Godot version 4.3  
- Retrodiffusion for asset generation. Godot for asset management?
- Claude code for programming

### **Genre** {#genre}

Singleplayer, roguelite, puzzle

### **Target Audience** {#target-audience}

This game is marketed to puzzle game players and, aspirationally, incremental game players who would like to play a short delineated roguelite 'run' that translates to roughly one series of related puzzles, then allows them to play a bit of home-base sims.

## **Concept** {#concept}

### **Gameplay overview** {#gameplay-overview}

The player controls the Alchemist's Apprentice, who has a stamina bar that lets him journey into a wilderness region. His primary mode of interacting with the world is his inventory, which is a grid of inventory slots. The player gathers 'reagents' from encounters and arranges them into their inventory slots, negotiating each reagent's unique set of boons and restrictions and each inventory slot's set of boons and restrictions. They player's goal on any 'run' is to complete a particular quest, which is usually some reagent-based challenge to the player's ability to meet certain requirements (eg, they must get X 'fire'-aspected items, or must retrieve a reagent that is particularly fragile and volatile).

---

The player is ultimately trying to complete a quest (to retrieve a specific reagent or set of aspects), or just trying to gather valuable ingredients. The player spends 'stamina' to venture further and must maintain enough stamina to return with their haul of gathered reagents. Meanwhile the aspects of reagents may also weigh on their stamina (eg, if they are too cold, they might use more stamina with each movement, or perhaps 'cold' just penalizes stamina similar to Peak, and that stamina is inaccessible until the player is not 'cold').

The player's quest may involve, for example, needing to bring home lots of hot things, or retrieve a reagent that is very unstable (meaning they must have a good balance of aspects in order to be able to carry it safely (eg, without it breaking or without it harming them.)

The player will also have different kinds of inventory space, similar to Backpack Battler, with bags that impose different kinds of inventory restrictions or boon. (Eg, a 'null' bag, which disables aspects of reagents stored in it, or a 'gravity' bag in which reagents fall to the bottom of the bag rather than being arrangeable as the player sees fit).

(Aspirationally) The player will then use their rewards and haul to upgrade their home, upgrade the bags available to them, unlock new quests to pursue, and so on. Base management may use a similar grid to the inventory system. You are always excited to buy more cabinets and drawers, which have more inventory slots for your base.
  Perhaps certain bags can be rented or are lent to the player depending on the quest, as well?

(Aspirationally) The player can unlock new regions to visit to gather ingredients (and do other quests in). This might be fluffed as the player acquiring gear that allows visiting that region (or perhaps 'inaccessible' regions will be visitable even before that, but de-facto locked to all except the more persistent and experienced players on account of nigh-insurmountable environmental hazards. Darkness, poison miasma, extreme heat or cold, etc.)

(Aspirationally) Beyond merely collecting ingredients, the player's encounters might have random events. Doubly-aspirationally, these might bear on an ongoing narrative, perhaps where the main character is an apprentice alchemist at an academy or in a town. Similar to Pokemon Mystery Dungeon or loosely similar to Stardew Valley.

Ultimately the idea is to have clear permanent progression as a reward for performing 'runs', while the runs themselves should feel like self-contained, relatively short puzzles. The fun is in trying to eke out a bit more value out of your inventory space, which means that bringing home more rarer or more temperamental reagents should have clear mechanical rewards.

(Aspirationally) There might also be a puzzle mode where the player is just given a predefined set of reagents and bags to arrange and needs to make them fit?

### **Primary Mechanics** {#primary-mechanics}

- Inventory grid
  - Items reside in the player's inventory, taking up inventory slots according to their placement
  - The player can expand their inventory with other 'bags', similar to Backpack Battler
  - Different bags may have their own boons or requirements, examples:
    - 'Null' bag nullifies aspects of contained reagents, eg negating 'volatile'
	- 'Locked' bag prevents reagents in the bag from being moved or discarded after placement
	- 'Gravity' bag pulls reagents down to the bottom of the bag
- Reagents
  - The items the player is trying to collect for money and quest requirements
  - Reagents have weight, shape, and aspects
    - Shape is what slots the item takes up. The item can be rotated but (usually) not rearranged
	- Weight is a restriction on how much the player can hold, separate from their grid
	- Aspects are alchemical properties of the reagent. These can be helpful or harmful (or benign unless another reagent with a reactive property is placed next to it). Examples:
	  - Elemental (fire, water, ice, earth, nature, metal, air, light, dark)
	  - Alchemical (albedo, nigredo, citrinitas, rubedo)
	  - Physical (fragile, orientation (must be vertical/horizonal/pointed up), volatile, toxic)
	  - Other categories?
  - Alchemical Codex tracks reagents and their aspects as the player runs afoul of them 
- Stamina
  - The player's ability to venture forth. Traversing from one slay-the-spire 'room' to the next room takes 1(?) stamina.
  - The player must maintain enough stamina to return home (they can collect reagents on the way)
  - The state of the player's inventory bears on their stamina (possibly restricting their stamina directly like in Peak? eg, 5 heat = 5 less stamina, but can be regained by discarding hot reagents/acquiring cold reagents? Penalties causing them to consume more stamina depending on the inventory? Long term (extending across multiple ventures) bonuses and maluses?)

### **Aspirational Mechanics** {#aspirational-mechanics}

- Permanent progression 
  - save reagents and bring them on ventures
  - save inventory bags and bring them on ventures
  - stamina upgrades
  - other powers/bonuses (eg 'cold-blooded') (alchemy themed? Consumable potions with x number of venture effects? Transformative potions with permanent effects?)
- Long term/permanent bonuses/maluses from alchemical Effects
  - increase Stamina
  - get sick or something, or elementally contaminated
  - immunity to 1000 poisons
- Special encounter types (beyond just 'here's a reagent, put it in your bag if you dare')
- Base upgrading (and at-home inventory storage/management)
- Story mode
- Puzzle only mode (just find a way to put all these reagents together without them exploding)
  - (Or maybe START with the puzzle mode and make the rest of the game after, lmao)

## **Art** {#art}

### **Design** {#design}

RetroDiffusion pixel art with an emphasis on cute/chibi designs. Recettear-like adventuring UI and main character. Item and aspect designs predicated on actual alchemical texts (eg, for the in-game codex), but with chibi-fied versions for items and inventory sprites. Inspiration from Core Keeper, Pokemon Mystery Dungeon, Stardew Valley, Recettear, Book of Hours (aspect system)

## **Audio** {#audio}

### **Music** {#music}

TBD

### **Sound Effects** {#sound-effects}

TBD

## **Game Experience** {#game-experience}

### **UI** {#ui}

Currently imagining this being pixelated still, but much higher res than the game (the pixelation is purely style fluff). Maid menu and top level UI is trying to be cute-ified alchemical texts. Possible theming around courier/alchemist holding parchment missives or keeping a codex and journal.

### **Controls** {#controls}

**Mouse & Keyboard**  
	Click to place, click confirm, scroll wheel to rotate
		Or click+drag to place
	Click + Arrow keys / WASD + r to rotate
		[Select key] + Arrow keys / WASD + r to rotate
	Fully customizeable
**Gamepad**  
	Dpad to move item around the bag/navigate menus, X/A/Start to place/confirm, Shoulder buttons to rotate

## **Development Timeline** {#development-timeline}

**MINIMUM VIABLE PRODUCT**

TBD

**BEYOND**

TBD