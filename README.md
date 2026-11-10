# Visual Progression Framework

A RimWorld 1.7 framework that lets items and buildings keep the look of the tech age they were made in. Something
made in the medieval age still looks medieval later on, and the same item made in the industrial age
looks industrial. Old items keep their look until they're used up.

[Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3834891951)


## How it works

Other mods list items and buildings and the art each one uses up to a given age. When a bench finishes a listed item,
or a listed building is built, the framework reads your colony's tech level and gives it that age's look. It saves the
look and keeps it.

The framework changes nothing by itself. Something also has to raise your colony's tech level, such as
Vanilla Factions Expanded - Tribals.

## Adding your items

Write one `ThingStyleDef` for each look, then list the item's looks in an `ItemLooksDef`:

```xml
<Defs>
  <ThingStyleDef>
    <defName>MyMod_MealSimple_Medieval</defName>
    <graphicData>
      <texPath>Things/Item/Meal/MySimpleMeal</texPath>
      <graphicClass>Graphic_StackCount</graphicClass>
    </graphicData>
  </ThingStyleDef>

  <VisualProgression.ItemLooksDef>
    <defName>MyMod_MealSimple</defName>
    <item>MealSimple</item>
    <looks>
      <li><upToAge>Medieval</upToAge><style>MyMod_MealSimple_Medieval</style></li>
    </looks>
  </VisualProgression.ItemLooksDef>
</Defs>
```

A look that leaves out its `graphicClass` takes its item's, such as `Graphic_StackCount` for medicine or `Graphic_Multi`
for furniture that rotates, so its art must be laid out the way that class expects. Meals use `Graphic_MealVariants`,
which wants a set of images for mixed, meat and vegetarian meals, so a meal look with plain stack images gives
`Graphic_StackCount` itself, as above. A look shared by items with different graphic classes needs its own
`graphicClass` too.

For an item from another mod, give the `ItemLooksDef` `MayRequire="<that mod's packageId>"`, so it's skipped when that
mod isn't active. Lists added as a `VisualProgression.CompProperties_AgeLook` in the item's `comps`, the way packs for
the first version did, still work.

An item uses the row with the lowest `upToAge` that covers its age: your colony's, or for trader goods and pawns' gear,
their faction's. Past the last row it keeps its normal art. When several mods list looks for the same item, their lists
merge. For the same `upToAge`, a row from an `ItemLooksDef` beats a row patched into `comps`, and among each kind the
mod loaded last wins. An item the game makes at startup, like meat or corpses, can't take looks.

Items made by bills get the look of your colony's age, and so do items a surgery produces, like hemogen packs.
Body parts and implants a surgery removes keep their normal art. Items a job makes also get your colony's age when the
job's code names the item, like the hemogen packs Vanilla Races Expanded - Sanguophage's extraction jobs make. The
framework finds those jobs when the game starts, so a mod needs nothing extra for it. The age is your colony's even
when another faction's pawn does the job. A job that takes the item from its settings instead, and things buildings
produce on their own, like beer from a fermenting barrel, keep their normal art. Items a new game starts with, from the scenario and
your colonists starting possessions, get the look of the age your colony starts in. Items traders sell, and the
clothes, weapons and inventory of pawns from other factions, get the look of that faction's age. Orbital traders, pawns
with no faction, raid loot, quest rewards and items found on the map keep their normal art. If an ideology gives the
item a style, that style wins.

Buildings get the look of their builder's age when construction finishes: your colony's, or for a building another
faction puts up, that faction's. A rebuilt or copied building takes the current age too. While you place a building
and while its blueprint waits, it shows the look of the age it was placed in, though it's built in the age it's
finished in. A frame shows the game's plain frame. Buildings
a new game starts with get the age your colony starts in, or their own faction's, buildings placed in god mode get
your colony's, and furniture made at a bench gets your colony's age like any item. Furniture bought from traders and
buildings on faction bases, ruins and quest sites keep their normal art.

The look is held in the item's style slot, the same one ideology styles use, so an item can have one or the other.
Most weapons and some apparel already have that slot. A listed item without one gets it. When Ideology weighs a
room's styles, a thing with a look counts as having no style, so it doesn't upset colonists who care about style.

For apparel, also give the look's `ThingStyleDef` a `wornGraphicPath`, and pawns wear the look too. Like the item's own
worn art, it needs one texture per body type (`_Male`, `_Female`, `_Thin`, `_Fat`, `_Hulk`), except headgear, eyewear
and packs. Without it, the item has the look on the ground and the normal art when worn.

To rename an item for an age, give the look's `ThingStyleDef` an `overrideLabel`. The item shows that name wherever it
appears on its own: on the map, when inspected, in stacks and in inventories. The name replaces the whole label, so an
item made from a material loses that part, such as "wooden". Places that name the item type, like storage filters and
info cards, follow your colony's age the way the type's icon does (below).

A retexture mod that puts its art at the game's own path hides the original, whatever the load order. To use the art
from one mod's files instead, make the look's graphic data a `VisualProgression.GraphicData_FromMod`. Without a
`fromMod`, the art comes from the mod whose Defs files add the item, here Biotech. An item another mod adds through a
patch has no mod of its own, so its look names the mod:

```xml
<ThingStyleDef>
  <defName>MyMod_HemogenPack_Plain</defName>
  <overrideLabel>hemogen pack</overrideLabel>
  <graphicData Class="VisualProgression.GraphicData_FromMod">
    <texPath>Things/Item/Resource/HemogenPack</texPath>
  </graphicData>
</ThingStyleDef>
```

To take the art from another mod, name it in `fromMod` by packageId: `Ludeon.RimWorld` for the base game,
`Ludeon.RimWorld.Biotech` and the like for a DLC, or any mod's. A look shared by items from different mods names its
mod too. Art from a mod works with `Graphic_Single`, `Graphic_StackCount`, `Graphic_Random` and `Graphic_Multi` (the
four facings most buildings and furniture use), masks included, so a material colours the parts that mod's mask
marks. A `maskPath` of the look's own is read from that mod too, or found as usual when that mod doesn't have it. It
covers the look's own graphic, its blueprint and placing preview, and packed furniture: a `wornGraphicPath` or
`uiIconPath` still shows whichever mod's art wins, and so do walls and other art that joins its neighbours. When that mod isn't active or
has no texture at the path, the look shows the normal art and the game logs a warning.

Icons in lists, like the gear tab and trade rows, show a styled item's full stack image at any stack size. Items on the
map show the right image for their stack.

The item's icon in bills, the research tab and info cards follows your colony's age, so it shows what the item would
look like if made now. Its name follows too in the research tab, storage filters, info cards and a bill's products, but
not in the bill's own name ("Make ..."), which the game sets once at startup. The name comes from the age's look when it
has an `overrideLabel`, otherwise it's the item's normal name. Items keep the name of the age they were made in, and
items without a look keep the normal name, except in rows that count several stacks of a type together, like a
transport pod's contents. A material without a `stuffAdjective` keeps its name, because things made from it carry it.
These update when a game starts or loads, and the moment Vanilla Factions Expanded - Tribals or
Node Research changes your colony's age. Without either of them, it checks about once an in game hour. Items with
their own icon file (`uiIconPath` or `uiIconPathsStuff`) keep their normal icon there, because copies without a look
would show the swapped icon too. The fuel icon on refuelable buildings and the medical care icons follow your colony's
age the same way. Buttons for a real item, like taking a drug, using an item or an apparel ability, show that item's
own look. A look drawn through a mask (`CutoutComplex`) shows its mask in icons and on those buttons too, as items without a look
do. The type's icon leaves it out for items made from a material and for looks with their own icon file.

To test your rows, turn on dev mode and use "Set colony tech level..." under Visual Progression in the debug actions
menu. Icons and names switch to the new age at once. Raise it one age at a time, because Vanilla Factions Expanded - Tribals
overshoots on bigger jumps. The level isn't saved unless a mod like that saves it.

## License

MIT
