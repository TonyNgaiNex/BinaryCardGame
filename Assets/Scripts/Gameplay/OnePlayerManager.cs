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

namespace Nex.BinaryCard
{
    public class OnePlayerManager : MonoBehaviour
    {
        [SerializeField] OnePlayerDetectionEngine onePlayerDetectionEngine;
        [SerializeField] int initHealth;
        [SerializeField] TextMeshProUGUI statText;
        [SerializeField] Transform cardLeftContainer;
        [SerializeField] Transform cardRightContainer;
        [SerializeField] List<CardBase> initialCards;
        [SerializeField] float threshold;
        [HideInInspector]public int health;
        int shield;
        PlayerState currentPlayerState;
        UniTaskCompletionSource<PlayerAnswer> _playerAnswer;
        Queue<float> queue = new Queue<float>();
        int playerIndex;
        public void Initialize(int aPlayerIndex, BodyPoseDetectionManager aBodyPoseDetectionManager)
        {
            onePlayerDetectionEngine.Initialize(aPlayerIndex, aBodyPoseDetectionManager);
            playerIndex = aPlayerIndex;
            onePlayerDetectionEngine.NewDetectionCapturedAndProcessed += ProcessPose;

            health = initHealth;
            shield = 0;
            statText.text =$"Health: {health}, Shield: {shield}";
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
            if (currentPlayerState != PlayerState.AwaitAnswer) return;
            var dpi = onePlayerDetectionEngine.DistancePerInch;
            var chestX = onePlayerDetectionEngine.Chest.position.x;
            var leftDistance = chestX-onePlayerDetectionEngine.LeftHand.position.x;
            var rightDistance = onePlayerDetectionEngine.RightHand.position.x-chestX;
            leftDistance/=dpi;
            rightDistance/=dpi;
            var add = leftDistance - rightDistance;
            queue.Enqueue(add);
            queue.Dequeue();
            if(queue.Average()>threshold )
                _playerAnswer.TrySetResult(PlayerAnswer.Left);
            if(queue.Average()<-threshold )
                _playerAnswer.TrySetResult(PlayerAnswer.Right);
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
            var leftCard = Instantiate(leftCardPrefab, cardLeftContainer);
            var rightCard = Instantiate(rightCardPrefab, cardRightContainer);

            leftCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Left);
            });
            rightCard.button.onClick.AddListener(() =>
            {
                _playerAnswer.TrySetResult(PlayerAnswer.Right);
            });

            _playerAnswer = new UniTaskCompletionSource<PlayerAnswer>();
            currentPlayerState = PlayerState.AwaitAnswer;
            queue = new Queue<float>(Enumerable.Repeat(0f, 10));
            var playerAnswer = await _playerAnswer.Task;
            currentPlayerState = PlayerState.None;

            Destroy(leftCard.gameObject);
            Destroy(rightCard.gameObject);

            if(playerAnswer==PlayerAnswer.Left)return leftCard;
            else return rightCard;
        }

        void UpdateStatus()
        {
            if (health <= 0) statText.text = "dead";
            else statText.text =$"Health: {health}\nShield: {shield}";
        }
    }

    public enum PlayerState
    {
        None,
        AwaitAnswer,
    }

    public enum PlayerAnswer
    {
        Left,
        Right,
    }
}
