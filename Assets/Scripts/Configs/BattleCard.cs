using System;
using System.Collections.Generic;

namespace Nex.BinaryCard
{
    public class BattleCard : CardBase
    {
        public List<BattleEffect> cardEffects;
    }
    [Serializable]
    public class BattleEffect
    {
        public BattleAction action;
        public BattleTarget target;
        public int value;
    }
    public enum BattleTarget
    {
        Self,
        AllPlayer,
        LowestHealthPlayer,
        RandomEnemy,
        LowestHealthEnemy,
    }

    public enum BattleAction
    {
        Attack,
        Shield,
        Heal,
        Charge,
    }
}
