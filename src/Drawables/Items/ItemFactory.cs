using System;

namespace CoachDraw.Drawables.Items
{
    public static class ItemBuilder
    {
        public static Item Build(Item.TypeEnum itemType, int? playerNumber)
        {
            return itemType switch
            {
                Item.TypeEnum.Offensive => new Offensive(playerNumber),
                Item.TypeEnum.Defensive => new Defensive(playerNumber),
                Item.TypeEnum.Winger => new Winger(playerNumber),
                Item.TypeEnum.Center => new Center(playerNumber),
                Item.TypeEnum.Defenseman => new Defenseman(playerNumber),
                Item.TypeEnum.Pylon => new Pylon(),
                Item.TypeEnum.Puck => new Puck(),
                Item.TypeEnum.Pucks => new Pucks(),
                Item.TypeEnum.Coach => new Coach(playerNumber),
                Item.TypeEnum.None => new None(playerNumber),
                Item.TypeEnum.DefensiveInt => new DefensiveInt(),
                Item.TypeEnum.PlayerNumber => new PlayerNumber(playerNumber ?? throw new Exception("Player number is required")),
                Item.TypeEnum.Goalie => new Goalie(),
                _ => throw new Exception(@"Unreachable")
            };
        }
    }
}