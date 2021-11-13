using System.Collections.Generic;

namespace CoachDraw.Drawables.Items.Definitions
{
    public sealed class ItemDefinitionFactory
    {
        public ItemDefinitionFactory()
        {
            // TODO DI these
            Values = new Dictionary<Item.TypeEnum, ItemDefinition>
            {
                {Item.TypeEnum.Center, new ItemDefinitionCenter()},
                {Item.TypeEnum.Coach, new ItemDefinitionCoach()},
                {Item.TypeEnum.Defenseman, new ItemDefinitionDefenseman()},
                {Item.TypeEnum.Defensive, new ItemDefinitionDefensive()},
                {Item.TypeEnum.DefensiveInt, new ItemDefinitionDefensiveInt()},
                {Item.TypeEnum.Goalie, new ItemDefinitionGoalie()},
                {Item.TypeEnum.None, new ItemDefinitionNone()},
                {Item.TypeEnum.Offensive, new ItemDefinitionOffensive()},
                {Item.TypeEnum.PlayerNumber, new ItemDefinitionPlayerNumber()},
                {Item.TypeEnum.Puck, new ItemDefinitionPuck()},
                {Item.TypeEnum.Pucks, new ItemDefinitionPucks()},
                {Item.TypeEnum.Pylon, new ItemDefinitionPylon()},
                {Item.TypeEnum.Winger, new ItemDefinitionWinger()}
            };
        }

        public Dictionary<Item.TypeEnum, ItemDefinition> Values { get; }
    }
}