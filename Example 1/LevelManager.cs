using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace Melai.LevelManager
{
    /// <summary>
    /// The level manager manages the loading of the level scene, 
    /// saving the progress of the passage and the current level, 
    /// draws buttons with levels and updates them when the progress of the passage changes
    /// </summary>
    public class LevelManager : MonoSingleton<LevelManager>
    {
        /// <summary>
        /// The level class is needed to store information about the level
        /// </summary>
        [System.Serializable]
        public class Level
        {
            [SerializeField]
            protected string name;                            // Level name
            [SerializeField]
            protected string scenePath;                       // Path to the scene that will be loaded when this level is selected  
            [SerializeField]
            protected GameObject buttonGO;                      // Level button
            public enum LevelState
            {
                Locked,
                Unlocked
            }
            public LevelState currentState;                 // Current state availability status
            public enum ResultPassing
            {
                NotPassed,
                Low,
                Middle,
                High
            }
            public ResultPassing currentResult;             // Current result of passing the level

            public string Name
            {
                get
                {
                    return name;
                }
            }                           // Property for getting the name of the level
            public string ScenePath
            {
                get
                {
                    return scenePath;
                }
            }                      // Property for getting the scenePath of the level
            public GameObject ButtonGO
            {
                get
                {
                    return buttonGO;
                }
            }                           // Property for getting the button of the level

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">Level name</param>
            /// <param name="scenePath">Path to the scene that will be loaded when this level is selected  </param>
            /// <param name="currentState">Current state availability status</param>
            /// <param name="currentResult">Current result of passing the level</param>
            public Level(string name, string scenePath, LevelState currentState, ResultPassing currentResult, GameObject buttonGO)
            {
                this.name = name;
                this.scenePath = scenePath;
                this.currentState = currentState;
                this.currentResult = currentResult;
                this.buttonGO = buttonGO;
            }
        }

        public bool useAutoGeneration;                                  // use auto generation of level buttons

        public Transform levelBox;                                      // box for placing levels
        public GameObject levelButtonPrefab;                            // level button prefab
        public List<Level> levelList = new List<Level>();               // full level list of game
        private List<Level> playerLevelList;                            // total player level list (possible game levels plus player's saved levels)
        private Level currentLevel;                                     // the current active level
        [SerializeField]
        private float delayLoad;                                        // delay before loading the selected level

        public delegate void OnLoading();
        public static event OnLoading onLoading;                        // the event that we call when the level is loading
        public static event OnLoading beforeLoadingScene;               // the event that we call before loading scene of the current level
        public static event OnLoading afterLoadingScene;                // the event that we call after loading scene of the current level
        public static event OnLoading onErrorLoading;                   // the event that we call when the level cannot be loading
        public static event OnLoading onErrorLoadingScene;              // the event that we call when the scene cannot be loading
        public static event OnLoading onErrorNextLevelLoading;          // the event that we call when the next level cannot be loading
        public static event OnLoading curLevelIsFinal;                  // the event that we call when the current level was final
        public static event OnLoading onErrorSelectedLevelLoading;      // the event that we call when the selected level cannot be loading
        
        public delegate void OnFinished(Level.ResultPassing result);
        public static event OnFinished onFinished;                       // the event that we call when the level is finished

        public string DefaultLevelName                                  // returns the default level name (the name of the level that starts when you first enter the game)
        {
            get
            {
                return levelList[0].Name;
            }
        }                                                              
        public string CurLevelName                                      // name of the currently active level
        {
            get
            {
                return currentLevel.Name;
            }
        }                                   

        private void Start()
        {
            DontDestroyOnLoad(gameObject);                              // call DontDestroyOnLoad for gameObject (Level Manager)

            LevelData.Create();                                         // create an instance with saved data

            PrepareLevelManager();
        }

        /// <summary>
        /// Set current level
        /// </summary>
        public void SetCurrentLevel()
        {
            Dictionary<string, int> savedLevels = LevelData.Instance.levels;  // getting saved levels

            // set current level
            if (savedLevels.Count == 1)
            {
                if ((savedLevels[savedLevels.Keys.First()] == (int)Level.ResultPassing.NotPassed) || (LevelData.Instance.currentLevel != DefaultLevelName))
                {
                    currentLevel = FindLevelByName(playerLevelList, LevelData.Instance.currentLevel);
                }
                else
                {
                    currentLevel = FindNextLevelByName(playerLevelList, LevelData.Instance.currentLevel);
                }
            }
            else if (savedLevels.Count == levelList.Count)
            {
                currentLevel = FindLevelByName(playerLevelList, LevelData.Instance.currentLevel);
            }
            else
            {
                currentLevel = FindLevelByName(playerLevelList, LevelData.Instance.currentLevel);
            }
            
            if (currentLevel == null)
            {
                throw new MissingReferenceException("Current level was not set!");
            }
        }

        /// <summary>
        /// Form player's level list (possible game levels plus player's saved levels)
        /// </summary>
        public void FormPlayerList()
        {
            // clearing the list of levels for the player
            playerLevelList.Clear();

            // copy full level list of game
            foreach (Level lvl in levelList)
            {
                Level newLevel = new Level(lvl.Name, lvl.ScenePath, lvl.currentState, lvl.currentResult, lvl.ButtonGO);
                playerLevelList.Add(newLevel);
            }

            Dictionary<string, int> savedLevels = LevelData.Instance.levels;    // getting saved levels

            string nameLastSavedLevel = savedLevels.Keys.Last();                // getting the last saved level
            bool setNextUnlocked = false;                                       // trigger for setting last available level            

            // form a summary list with levels for the player
            // we update the statuses of the levels in the player's level list according to the saved levels and make the level following the last saved level available for passing
            foreach (Level lvl in playerLevelList)
            {
                // if the level is among the saved ones, then we change its passing status from default to the one that is saved
                if (savedLevels.ContainsKey(lvl.Name))
                {
                    lvl.currentState = Level.LevelState.Unlocked;
                    int savedResult = savedLevels[lvl.Name];
                    lvl.currentResult = (Level.ResultPassing)savedResult;

                    // if the level is the last among the saved ones and has a status of passing any other than Level.ResultPassing.NotPassed, then set the variable setNextUnlocked to true
                    if (lvl.currentResult != Level.ResultPassing.NotPassed && lvl.Name == nameLastSavedLevel)
                    {
                        setNextUnlocked = true;
                    }
                }
                else if (setNextUnlocked)       // if setNextUnlocked is true, change the status of availability and passing the level
                {
                    lvl.currentState = Level.LevelState.Unlocked;
                    lvl.currentResult = Level.ResultPassing.NotPassed;
                    setNextUnlocked = false;
                }
                else
                {
                    break;                      // stop passing through the list
                }
            }
        }

        /// <summary>
        /// Prepare level manager
        /// </summary>
        public void PrepareLevelManager()
        {
            // create and form content for playerLevelList
            playerLevelList = new List<Level>();
            FormPlayerList();

            try
            {
                // try to set current level
                SetCurrentLevel();
            }
            catch (MissingReferenceException ex)
            {
                Debug.Log(ex.Message);

                // during development only
                /*Scene scene = SceneManager.GetActiveScene();
                try
                {
                    currentLevel = FindLevelByScenePath(playerLevelList, scene.path);
                    Debug.Log("Testing!");
                }
                catch (MissingReferenceException exception)
                {
                    Debug.Log(exception.Message);
                }*/

            }

            if (useAutoGeneration)
            {
                // draw level list
                DrawLevelList();
            } else
            {
                // set the current state of the buttons already in the scene
                PrepareLevelListButtons();
            }            
        }

        /// <summary>
        /// Load level after click on btn Start game
        /// </summary>
        public void LoadLevelByStartGame()
        {
            SetCurrentLevel();
            LoadLevel();
        }

        #region Level Management
        /// <summary>
        /// Load level
        /// </summary>        
        public void LoadLevel()
        {
            // if current level is null, call the event (onErrorLoading)
            if (currentLevel == null)
            {
                Debug.Log("The current level cannot be loaded");
                onErrorLoading?.Invoke();
                return;
            }

            // save current level
            LevelData.Instance.currentLevel = currentLevel.Name;
            LevelData.Instance.Save();

            LoadSceneByPath(currentLevel.ScenePath);
        }

        /// <summary>
        /// Repeat current active level
        /// </summary>
        public void RepeatLevel()
        {
            LoadLevel();
        }

        /// <summary>
        /// Load selected level
        /// </summary>
        /// <param name="level">Name of selected level</param>
        public IEnumerator LoadSelectedLevel(string name)
        {
            // add a delay before loading the level
            yield return new WaitForSecondsRealtime(delayLoad);

            // looking for the selected level and if find - load it
            Level level = FindLevelByName(playerLevelList, name);
            if (level != null)
            {
                currentLevel = level;
                LoadLevel();
            }
            else
            {
                //if the selected level is not found, call the event (onErrorSelectedLevelLoading)
                onErrorSelectedLevelLoading?.Invoke();
                Debug.Log($"level named {name} a was not found");
            }            
        }
        
        /// <summary>
        /// Load next level
        /// </summary>
        public void LoadNextLevel()
        {
            // looking for the next level
            Level nextLevel = FindNextLevelByName(playerLevelList, currentLevel.Name);

            // if the next level is found, we change the current active level and call the level loading
            if (nextLevel != null)
            {
                currentLevel = nextLevel;
                LoadLevel();
            }
            else
            {
                //if the next level is not found, call the event (onErrorNextLevelLoading)
                onErrorNextLevelLoading?.Invoke();
                Debug.Log("The last level was finished! There is not a new next level.");
            }
        }

        /// <summary>
        /// Save level
        /// </summary>
        private void SaveLevel(Level level)
        {
            // change or add information about the passage of the level
            if (!LevelData.Instance.ChangeLevel(level.Name, (int)level.currentResult))
            {
                LevelData.Instance.AddLevel(level.Name, (int)level.currentResult);
            }

            // save changes
            LevelData.Instance.Save();
        }

        /// <summary>
        /// Save result of current level, update UI and unlock next level (use after finish) and etc.
        /// </summary>
        public void FinishLevel(Level.ResultPassing newResult)
        {
            // getting old result
            Level.ResultPassing oldResult = currentLevel.currentResult;

            // if the newest result is better than old - change the level button and current result for the current level
            if (oldResult < newResult) 
            {
                currentLevel.currentResult = newResult;

                if (useAutoGeneration)
                {
                    GameObject btnGO = levelBox.Find(currentLevel.Name).gameObject;
                    ChangeLevelButtonResult(btnGO, currentLevel.currentResult);
                } 
                else
                {
                    ChangeLevelButtonResult(currentLevel.ButtonGO, currentLevel.currentResult);
                }                
            }

            // save level
            SaveLevel(currentLevel);

            // call onFinished event
            onFinished?.Invoke(newResult);

            // lookinf for the next level
            Level nextLevel = FindNextLevelByName(playerLevelList, currentLevel.Name);
            
            // if the next level was found - to unlock it and save as current level
            if (nextLevel != null)
            {
                if (nextLevel.currentState == Level.LevelState.Locked)
                {
                    UnlockLevel(nextLevel);
                    UnlockButton(nextLevel);
                }

                LevelData.Instance.currentLevel = nextLevel.Name;
                LevelData.Instance.Save();
            }
            else
            {
                // if the next level is not found (currrent level was final), call the event (curLevelIsFinal)
                curLevelIsFinal?.Invoke();
                Debug.Log("The last level was finished! There is not a new next level");
            }
        }

        /// <summary>
        /// Unlock level
        /// </summary>
        /// <param name="level">Locked level</param>
        /// <returns>Unlocked level</returns>
        private void UnlockLevel(Level level)
        {
            level.currentState = Level.LevelState.Unlocked;
            level.currentResult = Level.ResultPassing.NotPassed;
        }

        #endregion

        #region UI
        /// <summary>
        /// Add UI buttons of levels
        /// </summary>
        public void DrawLevelList()
        {
            ClearLevelList();

            // draw level UI buttons
            for (int i = 0; i < playerLevelList.Count; i += 1)
            {
                Level level = playerLevelList[i];
                DrawButton(level, (i + 1).ToString());
            }
        }

        /// <summary>
        /// Destroy all level ui button with their game objects
        /// </summary>
        public void ClearLevelList()
        {
            foreach (Transform child in levelBox.transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Draw UI level button
        /// </summary>
        /// <param name="level"></param>
        private void DrawButton(Level level, string textForBtn)
        {
            // instantiate
            GameObject newButtonGO = Instantiate(levelButtonPrefab, levelBox.transform, false) as GameObject;
            newButtonGO.name = level.Name;

            // change text
            Text btnText = newButtonGO.GetComponentInChildren<Text>();
            btnText.text = textForBtn;

            // add or remove interoperability depending on the level of accessibility of the level
            Button newButton = newButtonGO.GetComponent<Button>();
            SetButtonCurrentState(newButton, level);

            // set stars for level button
            LevelButton levelButton = newButtonGO.GetComponent<LevelButton>();
            SetButtonCurrentResult(levelButton, level.currentResult);
        }

        /// <summary>
        /// Prepares UI level buttons that are already on the scene
        /// </summary>
        private void PrepareLevelListButtons()
        {
            // prepare level UI buttons
            for (int i = 0; i < playerLevelList.Count; i += 1)
            {
                Level level = playerLevelList[i];
                PrepareButton(level, (i + 1).ToString());
            }
        }

        /// <summary>
        /// Prepares UI level button that is already on the scene
        /// </summary>
        /// <param name="level"></param>
        /// <param name="textForBtn">New text for level button</param>
        private void PrepareButton(Level level, string textForBtn)
        {
            // change text
            Text btnText = level.ButtonGO.GetComponentInChildren<Text>();
            btnText.text = textForBtn;

            // add or remove interoperability depending on the level of accessibility of the level
            Button button = level.ButtonGO.GetComponent<Button>();
            SetButtonCurrentState(button, level);

            // set stars for level button
            LevelButton levelButton = level.ButtonGO.GetComponent<LevelButton>();
            SetButtonCurrentResult(levelButton, level.currentResult);
        }

        /// <summary>
        /// Set state for button (add or remove interoperability depending on the level of accessibility of the level)
        /// </summary>
        /// <param name="button">Button</param>
        /// <param name="level">Level</param>
        private void SetButtonCurrentState(Button button, Level level)
        {
            switch (level.currentState)
            {
                case Level.LevelState.Locked:
                    button.interactable = false;
                    //Debug.Log("Locked");
                    break;
                case Level.LevelState.Unlocked:
                    button.onClick.AddListener(() => StartCoroutine(LoadSelectedLevel(level.Name)));
                    //Debug.Log("Unlocked");
                    break;
            }
        }

        /// <summary>
        /// Set stars for level button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="currentResult"></param>
        private void SetButtonCurrentResult(LevelButton button, Level.ResultPassing currentResult)
        {
            switch (currentResult)
            {
                case Level.ResultPassing.NotPassed:
                    //Debug.Log("NotPassed");
                    break;
                case Level.ResultPassing.Low:
                    button.SetLevelStars(1);
                    //Debug.Log("Low");
                    break;
                case Level.ResultPassing.Middle:
                    button.SetLevelStars(2);
                    //Debug.Log("Middle");
                    break;
                case Level.ResultPassing.High:
                    button.SetLevelStars(3);
                    //Debug.Log("High");
                    break;
            }
        }

        /// <summary>
        /// Make the button playable
        /// </summary>
        /// <param name="level"></param>
        private void UnlockButton(Level level)
        {
            GameObject btnGO;

            if (useAutoGeneration)
            {
                // looking for the level button
                btnGO = levelBox.Find(level.Name).gameObject;
            }
            else
            {
                btnGO = level.ButtonGO;
            }            

            // add listener and interactable is true
            Button btn = btnGO.GetComponent<Button>();
            btn.interactable = true;
            btn.onClick.AddListener(() => StartCoroutine(LoadSelectedLevel(level.Name)));

            // update stars for button
            ChangeLevelButtonResult(btnGO, level.currentResult);
        }

        /// <summary>
        /// Change result of level (number of stars)
        /// </summary>
        /// <param name="btnGO"></param>
        /// <param name="resultPassing"></param>
        private void ChangeLevelButtonResult(GameObject btnGO, Level.ResultPassing resultPassing)
        {
            //Text btnText = btnGO.GetComponentInChildren<Text>();
            //btnText.text = resultPassing.ToString();

            LevelButton levelButton = btnGO.GetComponent<LevelButton>();
            switch (resultPassing)
            {
                case Level.ResultPassing.Low:
                    levelButton.SetLevelStars(1);
                    //Debug.Log("Low");
                    break;
                case Level.ResultPassing.Middle:
                    levelButton.SetLevelStars(2);
                    //Debug.Log("Middle");
                    break;
                case Level.ResultPassing.High:
                    levelButton.SetLevelStars(3);
                    //Debug.Log("High");
                    break;
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Searches for a level by name
        /// </summary>
        /// <param name="searchList">List of levels to search</param>
        /// <param name="name">The name of the level to find</param>
        /// <returns></returns>
        public static Level FindLevelByName(List<Level> searchList, string name)
        {
            if (name == "" || name == null)
            {
                return null;
            }

            Level level = searchList.Find(item => item.Name == name);
            return level;
        }

        /// <summary>
        /// Searches for the next level after the current one, whose name got
        /// </summary>
        /// <param name="searchList">List of levels to search</param>
        /// <param name="name">Name of received level</param>
        /// <returns>Next level</returns>
        public static Level FindNextLevelByName(List<Level> searchList, string name)
        {
            Level level = FindLevelByName(searchList, name);
            int levelIndex = searchList.IndexOf(level);
            if (levelIndex == (searchList.Count - 1)) // reach the last level
            {
                return null;
            }
            else
            {
                Level nextLevel = searchList[levelIndex + 1];
                return nextLevel;
            }
        }

        /// <summary>
        /// Searches for a level by scenePath of level
        /// </summary>
        /// <param name="searchList">List of levels to search</param>
        /// <param name="scenePath">The path of the scene</param>
        /// <returns></returns>
        public static Level FindLevelByScenePath(List<Level> searchList, string scenePath)
        {
            if (scenePath == "" || scenePath == null)
            {
                throw new MissingReferenceException("Current scenePath was not set!");
            }

            Level level = searchList.Find(item => item.ScenePath == scenePath);

            if (level == null)
            {
                throw new MissingReferenceException("Level with current scenePath was not found!");
            }

            return level;
        }
        #endregion

        #region Scene Management
        /// <summary>
        /// Load scene by index (with curtain)
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <returns></returns>
        private IEnumerator LoadSceneByIndex(int sceneIndex, float delay = 0f)
        {
            beforeLoadingScene?.Invoke();
            yield return new WaitForSecondsRealtime(delay);
            SceneManager.LoadScene(sceneIndex);
            yield return new WaitForSecondsRealtime(delay);
            afterLoadingScene?.Invoke();
        }

        /// <summary>
        /// Load scene by scene path
        /// </summary>
        /// <param name="scenePath"></param>
        public void LoadSceneByPath(string scenePath)
        {
            // get the scene index, if it is correct(greater than or equal to 0), then load the scene by index
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex >= 0)
            {
                onLoading?.Invoke();
                StartCoroutine(LoadSceneByIndex(buildIndex));
            }
            else
            {
                Debug.Log("Scene not found environment loaded scenes!");
                onErrorLoadingScene?.Invoke();
            }
        }
        #endregion
    }
}
