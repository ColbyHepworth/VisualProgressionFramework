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

An item uses the row with the lowest `upToAge` that covers your colony's age. Past the last row it keeps its normal art.

Only items made by bills get a look. Items from traders and loot keep their normal art. If an ideology gives the item
a style, that style wins.

Items that already have a styleable comp, like most weapons and some apparel, aren't supported yet. The game logs a
config error if you add looks to one.

Icons in lists, like the gear tab and trade rows, show a styled item's full-stack image at any stack size. Items on the
map show the right image for their stack.

To test your rows, turn on dev mode and use "Set colony tech level..." under Visual Progression in the debug actions
menu. Raise it one age at a time, because Vanilla Factions Expanded - Tribals overshoots on bigger jumps. The level
isn't saved unless a mod like that saves it.

## Building

Clone this repo into your RimWorld `Mods` folder and run `dotnet build Source`. The project finds the game's DLLs
three folders up. If your copy lives somewhere else, add `-p:GameDir="<your RimWorld folder>"`.

## License

MIT
