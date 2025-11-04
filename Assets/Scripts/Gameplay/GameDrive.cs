using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nex.BinaryCard
{
    public class GameDrive : MonoBehaviour
    {
        [SerializeField] DetectionManager detectionManager = null!;
        [SerializeField] PreviewsManager previewsManager = null!;
        [SerializeField] OnePlayerDetectionEngine detectionEnginePrefab = null!;
        [SerializeField] GameObject playersContainer = null!;
        [SerializeField] Transform enemyContainer;
        [SerializeField] EnemyBase enemyPrefab;
        [SerializeField]OnePlayerManager onePlayerManager = null!;
        [SerializeField] int numOfPlayers = 1;
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

            playersContainer.SetActive(false);
            for (var playerIndex = 0; playerIndex < numOfPlayers; playerIndex++)
            {
                var detectionEngine = Instantiate(detectionEnginePrefab, playersContainer.transform);
                detectionEngine.Initialize(playerIndex, detectionManager.BodyPoseDetectionManager);
                onePlayerManager.Initialize(detectionEngine);
            }

            await ScreenBlockerManager.Instance.Hide();

            await RunSetup();
            RunGame();
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

        void RunGame()
        {
            detectionManager.ConfigForGameplay();
            playersContainer.SetActive(true);
            RunBattle(enemyPrefab).Forget();
        }

        void RunMapDecision()
        {

        }

        async UniTask RunBattle(EnemyBase enemyBase)
        {
            var enemy = Instantiate(enemyPrefab, enemyContainer);
            enemy.Initialize(onePlayerManager);
            while (enemy.health > 0 && onePlayerManager.health > 0)
            {
                onePlayerManager.ResetShield();
                await onePlayerManager.Turn(enemy);
                await enemy.Turn();
            }
            Debug.Log("End Game");
        }

        void RunRest()
        {

        }
        #endregion
    }
}
