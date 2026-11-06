# Visual Progression Framework

A RimWorld 1.7 framework that lets items keep the look of the tech age they were crafted in. Something
made in the medieval age still looks medieval later on, and the same item made in the industrial age
looks industrial. Old items keep their look until they're used up.


## How it works

Other mods list items and the art each one uses up to a given age. When a bench finishes a listed item,
the framework reads your colony's tech level and gives the item that age's look. The item saves it and keeps it.

The framework changes nothing by itself. Something also has to raise your colony's tech level, such as
Vanilla Factions Expanded - Tribals.

## Adding your items

Write one `ThingStyleDef` for each look, then add the comp to the item:

```xml
<Defs>
  <ThingStyleDef>
    <defName>MyMod_MealSimple_Medieval</defName>
    <graphicData>
      <texPath>Things/Item/Meal/MySimpleMeal</texPath>
      <graphicClass>Graphic_StackCount</graphicClass>
    </graphicData>
  </ThingStyleDef>
</Defs>
```

```xml
<Patch>
  <Operation Class="PatchOperationConditional">
    <xpath>Defs/ThingDef[defName="MealSimple"]/comps</xpath>
    <match Class="PatchOperationAdd">
      <xpath>Defs/ThingDef[defName="MealSimple"]/comps</xpath>
      <value>
        <li Class="VisualProgression.CompProperties_AgeLook">
          <looks>
            <li><upToAge>Medieval</upToAge><style>MyMod_MealSimple_Medieval</style></li>
          </looks>
        </li>
      </value>
    </match>
    <nomatch Class="PatchOperationAdd">
      <xpath>Defs/ThingDef[defName="MealSimple"]</xpath>
      <value>
        <comps>
          <li Class="VisualProgression.CompProperties_AgeLook">
            <looks>
              <li><upToAge>Medieval</upToAge><style>MyMod_MealSimple_Medieval</style></li>
            </looks>
          </li>
        </comps>
      </value>
    </nomatch>
  </Operation>
</Patch>
```

The two branches are there because many items get their `comps` from a parent def, so the item may not have its own
`comps` node yet.

An item uses the row with the lowest `upToAge` that covers its age: your colony's, or for trader goods and pawns' gear,
their faction's. Past the last row it keeps its normal art. An item takes one list of looks. If a second mod adds
another, the game logs a config error and the second list is ignored.

Items made by bills get the look of your colony's age. Items a new game starts with, from the scenario and your
colonists' starting possessions, get the look of the age your colony starts in. Items traders sell, and the clothes,
weapons and inventory of pawns from other factions, get the look of that faction's age. Orbital traders, pawns with no
faction, raid loot, quest rewards and items found on the map keep their normal art. If an ideology gives the item a style, that
style wins.

The look is held in the item's style slot, the same one ideology styles use, so an item can have one or the other.
Most weapons and some apparel already have that slot. A listed item without one gets it.

Icons in lists, like the gear tab and trade rows, show a styled item's full stack image at any stack size. Items on the
map show the right image for their stack.

The item's icon in bills, the research tab and info cards follows your colony's age, so it shows what the item would
look like if made now. It updates when a game starts or loads, and the moment Vanilla Factions Expanded - Tribals or
Node Research changes your colony's age. Without either of them, it checks about once an in game hour. Items with
their own icon file (`uiIconPath` or `uiIconPathsStuff`) keep their normal icon there, because copies without a look
would show the swapped icon too.

To test your rows, turn on dev mode and use "Set colony tech level..." under Visual Progression in the debug actions
menu. Icons switch to the new age at once. Raise it one age at a time, because Vanilla Factions Expanded - Tribals
overshoots on bigger jumps. The level isn't saved unless a mod like that saves it.

## License

MIT
