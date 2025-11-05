using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cysharp.Threading.Tasks;
using Jazz;
using Nex;
using Nex.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nex.BinaryCard
{
    public class OnePlayerManager : MonoBehaviour
    {
        [SerializeField] OnePlayerDetectionEngine onePlayerDetectionEngine;
        [SerializeField] int initHealth;
        [SerializeField] TextMeshProUGUI statText;
        [SerializeField] Transform cardLeftContainer;
        [SerializeField] Transform cardRightContainer;
        [SerializeField] Slider leftSlider;
        [SerializeField] Slider rightSlider;
        [SerializeField] List<CardBase> initialCards;
        [SerializeField] float threshold;
        [HideInInspector]public int health;
        int shield;
        UniTaskCompletionSource<PlayerAnswer> _playerAnswer;
        int playerIndex;
        public void Initialize(int aPlayerIndex, BodyPoseDetectionManager aBodyPoseDetectionManager)
        {
            onePlayerDetectionEngine.Initialize(aPlayerIndex, aBodyPoseDetectionManager);
            playerIndex = aPlayerIndex;

            health = initHealth;
            shield = 0;
            statText.text =$"Health: {health}, Shield: {shield}";

            leftSlider.gameObject.SetActive(false);
            rightSlider.gameObject.SetActive(false);
        }

        public void Damage(int damage)
        {
            if(shield > damage)shield -= damage;
            else
            {
                damage -= shield;
                shield = 0;
                health -= damage;
            }
            UpdateStatus();
        }

        public void Shield(int amount)
        {
            shield += amount;
            UpdateStatus();
        }

        public void ResetShield()
        {
            shield = 0;
            UpdateStatus();
        }
        void ProcessPose(BodyPoseDetectionResult result)
        {
            var chestY = onePlayerDetectionEngine.Chest.position.y;
            var raiseLeft = onePlayerDetectionEngine.LeftHand.position.y>chestY;
            var raiseRight = onePlayerDetectionEngine.RightHand.position.y>chestY;

            leftSlider.value -= .5f*Time.deltaTime;
            rightSlider.value -= .5f*Time.deltaTime;

            if (raiseLeft)leftSlider.value += 1f*Time.deltaTime;
            if (raiseRight)rightSlider.value += 1f*Time.deltaTime;

            if (leftSlider.value >= 1) _playerAnswer.TrySetResult(PlayerAnswer.Left);
            if (rightSlider.value >= 1) _playerAnswer.TrySetResult(PlayerAnswer.Right);
        }

        public async UniTask BattleRound(EnemyBase enemyBase)
        {
            initialCards.Shuffle();
            var randomTwoCards = initialCards.Take(2).ToList();
            var chosenCard =  await PlayerChooseBetweenTwoOption(randomTwoCards[0], randomTwoCards[1]);
            await ProcessCard(chosenCard, enemyBase);
        }

        async UniTask ProcessCard(CardBase card, EnemyBase enemy)
        {
            Shield(card.shield);
            enemy.Damage(card.damage);
            await UniTask.Delay(TimeSpan.FromSeconds(2));
        }

        async UniTask<CardBase> PlayerChooseBetweenTwoOption(CardBase leftCardPrefab, CardBase rightCardPrefab)
        {
            // init left and right card
            var leftCard = Instantiate(leftCardPrefab, cardLeftContainer);
            var rightCard = Instantiate(rightCardPrefab, cardRightContainer);

            var leftEntryAnimation = leftCard.EntryAnimation();
            var rightEntryAnimation = rightCard.EntryAnimation();
            await UniTask.WhenAll(rightEntryAnimation, leftEntryAnimation);

            // Add button for debug usage
            leftCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Left);
            });
            rightCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Right);
            });

            // start the detection
            leftSlider.gameObject.SetActive(true);
            rightSlider.gameObject.SetActive(true);
            leftSlider.value = 0;
            rightSlider.value = 0;
            _playerAnswer = new UniTaskCompletionSource<PlayerAnswer>();
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed += ProcessPose;

            // finish detection
            var playerAnswer = await _playerAnswer.Task;
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed -= ProcessPose;
            leftSlider.gameObject.SetActive(false);
            rightSlider.gameObject.SetActive(false);

            var chosenCard = playerAnswer==PlayerAnswer.Left ? leftCard : rightCard;
            await chosenCard.ChosenAnimation();

            var leftExitAnimation = leftCard.ExitAnimation();
            var rightExitAnimation = rightCard.ExitAnimation();
            await UniTask.WhenAll(leftExitAnimation, rightExitAnimation);

            Destroy(leftCard.gameObject);
            Destroy(rightCard.gameObject);

            return chosenCard;
        }

        void UpdateStatus()
        {
            if (health <= 0) statText.text = "dead";
            else statText.text =$"Health: {health}\nShield: {shield}";
        }
    }

    public enum PlayerAnswer
    {
        Left,
        Right,
    }
}
