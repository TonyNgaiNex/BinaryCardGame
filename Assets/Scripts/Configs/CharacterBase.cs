using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Nex.BinaryCard
{
    public class CharacterBase:MonoBehaviour
    {
        [HideInInspector]public Dictionary<CharacterAttribute,int> characterAttributes;
        [SerializeField] int initHealth;
        [SerializeField] TextMeshProUGUI statText;
        [HideInInspector]public UnityEvent<BattleEffect,CharacterBase> processBattleEffect;

        public virtual void Initialize()
        {
            // Initialize player attribute
            characterAttributes = new Dictionary<CharacterAttribute, int>();
            foreach (CharacterAttribute attr in Enum.GetValues(typeof(CharacterAttribute)))
                characterAttributes[attr] = 0;
            characterAttributes[CharacterAttribute.Health] = initHealth;
            UpdateDisplayAttribute();
        }
        protected void UpdateDisplayAttribute()
        {
            //TODO make it actually a bar not a text
            if (characterAttributes[CharacterAttribute.Health] <= 0)
            {
                statText.text = "dead";
                return;
            }

            string attributeDisplayed="";
            foreach (var kvp in characterAttributes)
            {
                if (kvp.Value > 0) attributeDisplayed += $"{kvp.Key}: {kvp.Value}\n";
            }
            statText.text = attributeDisplayed;
        }
        public void ReceiveDamage(int damage)
        {
            if(characterAttributes[CharacterAttribute.Shield] > damage)
                characterAttributes[CharacterAttribute.Shield]  -= damage;
            else
            {
                damage -= characterAttributes[CharacterAttribute.Shield] ;
                characterAttributes[CharacterAttribute.Shield]  = 0;
                characterAttributes[CharacterAttribute.Health] -= damage;
            }

            if (characterAttributes[CharacterAttribute.Health] <= 0) Death();
            UpdateDisplayAttribute();
        }

        public void ReceiveShield(int amount)
        {
            characterAttributes[CharacterAttribute.Shield]  += amount;
            UpdateDisplayAttribute();
        }

        protected virtual void Death()
        {

        }
    }
    public enum CharacterAttribute
    {
        Health,
        Shield,
        Energy,
    }
}
