using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nex.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nex.BinaryCard
{
    public class GameDrive : MonoBehaviour
    {
        [SerializeField] DetectionManager detectionManager = null!;
        [SerializeField] PreviewsManager previewsManager = null!;
        [SerializeField] OnePlayerDetectionEngine detectionEnginePrefab = null!;
        [SerializeField] Transform playersContainer = null!;
        [SerializeField] Transform enemyContainer;
        [SerializeField] EnemyBase enemyPrefab;
        [SerializeField]OnePlayerManager onePlayerManagerPrefab = null!;
        [SerializeField] int numOfPlayers = 1;

        List<OnePlayerManager> players = new List<OnePlayerManager>();
        void Start()
        {
            if (Application.isEditor)
            {
                Application.runInBackground = true;
            }

            StartAsync().Forget();
        }

        async UniTask StartAsync()
        {
            detectionManager.Initialize(numOfPlayers);
            previewsManager.Initialize(numOfPlayers, detectionManager.CvDetectionManager,
                detectionManager.BodyPoseDetectionManager, detectionManager.PlayAreaController,
                detectionManager.SetupStateManager);

            for (int playerIndex = 0; playerIndex < numOfPlayers; playerIndex++)
            {
                var player = Instantiate(onePlayerManagerPrefab, playersContainer);
                player.Initialize(0, detectionManager.BodyPoseDetectionManager);
                players.Add(player);
            }

            await ScreenBlockerManager.Instance.Hide();

            await RunSetup();
            await RunGame();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(GameConfigsManager.Instance.MainScene);
            }
        }

        #region Setup

        async UniTask RunSetup()
        {
            detectionManager.ConfigForSetup();
            await previewsManager.MoveIn(true);

            previewsManager.SetPromptText("Move into the frame");
            await detectionManager.SetupStateManager.WaitForGoodPlayerPosition();

            previewsManager.SetPromptText("Raise your hand to start");
            detectionManager.SetupStateManager.SetAllowPassingRaisingHandState(true);
            await detectionManager.SetupStateManager.WaitForRaiseHand();

            await previewsManager.MoveOut(true);
        }

        #endregion

        #region Game

        async UniTask RunGame()
        {
            detectionManager.ConfigForGameplay();
            await RunInitialSelectCard();
            await RunBattle(enemyPrefab);
        }

        async UniTask RunInitialSelectCard()
        {
            //TODO when the game start let player select cards
            await UniTask.Yield();
        }

        async UniTask RunBattle(EnemyBase enemyBase)
        {
            var enemy = Instantiate(enemyPrefab, enemyContainer);
            enemy.Initialize(players);
            while (enemy.health > 0 && IsAllPlayerAlive())
            {
                RestAllPlayerShield();
                List<UniTask> playBattleTasks = new List<UniTask>();
                foreach (var player in players)
                {
                    var task = player.BattleRound(enemyBase);
                    playBattleTasks.Add(task);
                }
                await UniTask.WhenAll(playBattleTasks);
                await enemy.Turn();
            }
        }
        #endregion

        #region Players

        bool IsAllPlayerAlive()
        {
            foreach (var player in players)
            {
                if (player.health <= 0) return false;
            }
            return true;
        }

        void RestAllPlayerShield()
        {
            foreach (var player in players)
            {
                player.ResetShield();
            }
        }
        #endregion
    }
}
