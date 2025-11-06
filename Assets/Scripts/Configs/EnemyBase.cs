using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Nex.BinaryCard
{
    public class EnemyBase : CharacterBase
    {
        [SerializeField] TextMeshProUGUI intentionText;
        [SerializeField] List<BattleEffect> battleEffects = new List<BattleEffect>();
        int currentEffectIndex;
        public override void Initialize()
        {
            base.Initialize();
            UpdateBattleIntentionText(battleEffects[currentEffectIndex]);
        }

        public async UniTask BattleTurn()
        {

            processBattleEffect.Invoke(battleEffects[currentEffectIndex], this);
            currentEffectIndex++;
            if (battleEffects.Count <= currentEffectIndex) currentEffectIndex = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(2));
            UpdateBattleIntentionText(battleEffects[currentEffectIndex]);
        }

        void UpdateBattleIntentionText(BattleEffect battleEffect)
        {
            intentionText.text = battleEffect.action.ToString() + " " + battleEffect.target.ToString() + " " +
                                 battleEffect.value;
        }
    }
}
