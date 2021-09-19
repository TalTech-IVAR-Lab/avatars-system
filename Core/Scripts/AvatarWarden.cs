namespace Games.NoSoySauce.Avatars.AvatarSystem
{
    using System;
    using UnityEngine;
    using Games.NoSoySauce.Networking.Multiplayer;

    /// <summary>
    /// This script provides methods to spawn player avatar in game.
    /// </summary>
    public static class AvatarWarden
    {
        #region Data Types
        /// <summary>
        /// Collection of references to player rig elements.
        /// </summary>
        [Serializable]
        public class PlayerAvatarReferences
        {
            public GameObject avatar;
            public AvatarAssembler assembler;
            /* deprecated as rig is not used anymore 
            public GameObject rig;
            public RigAvatarLinker linker;
            */
            /*
            public GameObject rig_root;
            public GameObject rig_Head;
            public GameObject rig_LeftWrist;
            public GameObject rig_RightWrist;
            public InteractorFacade rig_LeftHandInteractor;
            public InteractorFacade rig_RightHandInteractor;
            public GameObject rig_AvatarContainer;
            public PlayerEntity playerEntity;
            public GameObject avatar;
            public AvatarAssembler avatar_Assembler;
            public Animator avatar_Animator;
            */
        }
        #endregion

        #region Configuration Variables
        /// <summary>
        /// <see cref="AvatarData"/> file contains the data required for spawning the avatar.
        /// </summary>
        public static AvatarData AvatarData { get; set; } = null;
        #endregion

        #region Runtime Variables
        /// <summary>
        /// Data structure containing references to important parts of currently spawned avatar.
        /// </summary>
        public static PlayerAvatarReferences LocalPlayerAvatar { get; private set; } = null;
        /// <summary>
        /// Whether the avatar is currently spawned.
        /// </summary>
        public static bool IsAvatarSpawned => (LocalPlayerAvatar?.avatar != null);
        #endregion

        #region Delegates and Events
        /// <summary>
        /// Event invoked before the avatar gets spawned.
        /// </summary>
        public static event Action<Pose> OnBeforeAvatarSpawned;
        /// <summary>
        /// Event invoked after the avatar gets spawned.
        /// </summary>
        public static event Action OnAfterAvatarSpawned;
        #endregion

        #region Public Methods
        /// <summary>
        /// Spawns player avatar using currently set <see cref="AvatarData"/>.
        /// </summary>
        /// <param name="spawnPose">Position and rotation where the avatar should be spawned.</param>
        public static void SpawnAvatar(Pose spawnPose)
        {
            // Run sanity checks.
            if (IsAvatarSpawned)
            {
                Debug.LogWarning("Will not spawn avatar: local player avatar is already spawned.", LocalPlayerAvatar.avatar);
                return;
            }
            if (AvatarData == null)
            {
                Debug.LogError("Cannot spawn avatar: " + nameof(AvatarData) + " reference is not set.");
                return;
            }
            /* deprecated as rig is not used anymore 
             if (AvatarData.rigPrefab == null)
            {
                Debug.LogError("Cannot spawn avatar: " + nameof(AvatarData.rigPrefab) + " reference is missing.", AvatarData);
                return;
            }
            */
            if (AvatarData.avatarPrefab == null)
            {
                Debug.LogError("Cannot spawn avatar: " + nameof(AvatarData.avatarPrefab) + " reference is missing.", AvatarData);
                return;
            }

            // Invoke before-spawned event.
            OnBeforeAvatarSpawned?.Invoke(spawnPose);

            // Set up avatar.
            var avatarInstance = MultiplayerManager.Instantiate(AvatarData.avatarPrefab, spawnPose.position, spawnPose.rotation);

            var assembler = avatarInstance.GetComponentInChildren<AvatarAssembler>();
            if (assembler == null)
            {
                Debug.LogError("Cannot assemble avatar: " + AvatarData.avatarPrefab + " does not have " + nameof(AvatarAssembler) + " script in it", AvatarData.avatarPrefab);
                return;
            }
            assembler.AssembleAvatar(AvatarData);

            /* deprecated as rig is not used anymore 
            // Set up rig and link avatar to it.
            var rigInstance = MultiplayerManager.Instantiate(AvatarData.rigPrefab, spawnPose.position, spawnPose.rotation);
            var linker = rigInstance.GetComponentInChildren<RigAvatarLinker>();
            if (linker == null)
            {
                Debug.LogError("Cannot link avatar: " + AvatarData.rigPrefab + " does not have " + nameof(RigAvatarLinker) + " script in it", AvatarData.rigPrefab);
                return;
            }
            linker.Link(avatarInstance);
            */

            // Store references in <see cref="LocalPlayerAvatar"/>.
            LocalPlayerAvatar = new PlayerAvatarReferences()
            {
                avatar = avatarInstance,
                assembler = assembler
            };

            // Invoke after-spawned event.
            OnAfterAvatarSpawned?.Invoke();
        }
        
        /// <summary>
        /// Destroys <see cref="LocalPlayerAvatar"/> if it was spawned before.
        /// </summary>
        public static void UnspawnAvatar()
        {
            // Run sanity checks.
            if (IsAvatarSpawned == false)
            {
                Debug.LogWarning("Cannot unspawn avatar: it is not spawned.");
                return;
            }

            // Destroy existing avatar.
            MultiplayerManager.Destroy(LocalPlayerAvatar.avatar);
            LocalPlayerAvatar = null;
        }
        #endregion

        /*
        #region Unity Callbacks
        private void Awake()
        {
            _instance = this;

            /// Check references.
            {
                if (playerRig_Prefab == null)
                {
                    EFDebug.LogError("Missing " + nameof(playerRig_Prefab) + " reference. Please set it up in the Inspector.", this, EFDebug.ScriptType.AvatarSystem);
                }
                if (playerAvatar_Prefab == null)
                {
                    EFDebug.LogError("Missing " + nameof(playerAvatar_Prefab) + " reference. Please set it up in Inspector.", this, EFDebug.ScriptType.AvatarSystem);
                }
            }
        }

        private void OnEnable()
        {
            EverfightSceneManager.OnLevelFinishedLoading += SpawnOnLevelLoaded;

            // TODO: Remove this later. Other scripts will spawn the player.
            SpawnPlayerAvatar();
        }

        private void OnDisable()
        {
            UnspawnPlayerAvatar();

            EverfightSceneManager.OnLevelFinishedLoading -= SpawnOnLevelLoaded;
        }

        private void OnDestroy()
        {
            _instance = null;
        }
        #endregion

        #region Methods (spawning)
        /// <summary>
        /// This method is called by AvatarAssembler script after the avatar was spawned and assembled on all clients.
        /// </summary>
        public static void FinishSpawn()
        {
            EFDebug.Log("Player avatar spawn complete.", Instance, EFDebug.ScriptType.AvatarSystem);

            /// Notify other scripts using event.
            OnPlayerAvatarSpawned?.Invoke();
        }

        /// <summary>
        /// Called when the scene was loaded by Unity.
        /// </summary>
        [System.Obsolete("Other scripts spawn avatar?")]
        private void SpawnOnLevelLoaded(LevelLoadingEventInfo e)
        {
            EFDebug.Log("'" + e.levelName + "' scene was loaded. Trying to spawn player here.", this, EFDebug.ScriptType.AvatarSystem);
            SpawnPlayerAvatar();
        }

        /// <summary>
        /// Spawns player avatar in a valid spawn point. If no spawn points found, does not spawn.
        /// </summary>
        public static void SpawnPlayerAvatar()
        {
            /// Removes another local avatar if one already exists.
            RemoveDuplicateAvatars();

            throw new System.NotImplementedException();
            //Debug.LogError("SPAWNING");

            /// Try to find spawn points.
            SpawnPointTest[] spawnPoints = null;//GetSpawnPoints();
            if (spawnPoints.Length == 0)
            {
                /// If no spawn points found, disable player physics/locomotion and do not spawn the avatar.
                EFDebug.Log("No spawn points found '" + EverfightSceneManager.ActiveSceneName + "' scene. Did you forget to add them? Spawning avatar in the origin.", Instance, EFDebug.ScriptType.AvatarSystem);
            }

            /// There are spawn points in the scene, we can spawn the player. So, enable player physics/locomotion.
            EFDebug.Log("Spawning local player avatar...", Instance, EFDebug.ScriptType.AvatarSystem);

            GameObject newPlayerRig;
            GameObject newPlayerAvatar;

            if (PhotonNetwork.InRoom)
            {
                //Debug.LogError("ONLINE");
                SpawnPointTest spawnPoint = null; // SoyVRTK_SpawnPoint.GetSpawnPointByOrdinalNumber(); //spawnPoints[GameManagerTest.GetMyOrdinalNumber()]; // TODO: Think about it
                newPlayerRig = PhotonNetwork.Instantiate(Instance.playerRig_Prefab.name, spawnPoint.transform.position, spawnPoint.transform.rotation);
                newPlayerAvatar = PhotonNetwork.Instantiate(Instance.playerAvatar_Prefab.name, spawnPoint.transform.position, spawnPoint.transform.rotation);
            }
            else
            {
                if (spawnPoints.Length == 0)
                {
                    newPlayerRig = Instantiate(Instance.playerRig_Prefab, Vector3.zero, Quaternion.identity);
                    //newPlayerAvatar = Instantiate(Instance.playerAvatar_Prefab, Vector3.zero, Quaternion.identity);
                }
                else
                {
                    //Debug.LogError("OFFLINE");
                    SpawnPointTest spawnPoint = spawnPoints[0];
                    newPlayerRig = Instantiate(Instance.playerRig_Prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                    //newPlayerAvatar = Instantiate(Instance.playerAvatar_Prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
            }

            //StorePlayerRig(newPlayerRig, newPlayerAvatar);
            //LocalPlayerRig.avatar_Assembler.AssembleAvatar();
            UpdatePresenceScriptsState();
        }

        /// <summary>
        /// Unspawns player avatar.
        /// </summary>
        public static void UnspawnPlayerAvatar()
        {
            if ((LocalPlayerRig == null) || (LocalPlayerRig.playerEntity == null)) return;

            //Debug.LogError("UNSPAWN");
            if (LocalPlayerRig.playerEntity != null)
            {
                //Debug.LogError("destroying", LocalPlayerRig.playerEntity);
                MultiplayerManager.NetworkSafeDestroy(LocalPlayerRig.playerEntity.gameObject);
            }

            LocalPlayerRig = null;
            //PlayerPresenceManager.TogglePlayerBodyPhysics(false);
            //PlayerPresenceManager.TogglePlayerLocomotion(false);
        }
        #endregion

        #region Methods (utility)
        /// <summary>
        /// Updates presence scripts according to current avatar state (<see cref="IsAvatarSpawned"/>).
        /// </summary>
        private static void UpdatePresenceScriptsState()
        {
            return;
            //PlayerPresenceManager.TogglePlayerBodyPhysics(IsAvatarSpawned);
            //PlayerPresenceManager.TogglePlayerLocomotion(IsAvatarSpawned);
        }

        /// <summary>
        /// Takes the newly spawned player rig and sets links to its parts into the static localPlayer variable.
        /// </summary>
        public static void StorePlayerRig(GameObject playerRigRootToStore, GameObject playerAvatarToStore)
        {
            if (!playerRigRootToStore || !playerAvatarToStore) return;

            /// Check if all elements are present to avoid missing anything.
            {
                if (playerRigRootToStore.GetComponent<PlayerEntity>() == null)
                {
                    EFDebug.LogError("Provided player rig does not have 'PlayerEntity' component! Please add it to this rig prefab root.", Instance, EFDebug.ScriptType.AvatarSystem);
                    return;
                }
                if (playerRigRootToStore.transform.Find(Config_AvatarStructure.Head) == null)
                {
                    EFDebug.LogError("Provided player rig must have a child transform named '" + Config_AvatarStructure.Head + "'.", Instance, EFDebug.ScriptType.AvatarSystem);
                    return;
                }
                if (playerRigRootToStore.transform.Find(Config_AvatarStructure.LeftWrist) == null)
                {
                    EFDebug.LogError("Provided player rig must have a child transform named '" + Config_AvatarStructure.LeftWrist + "'.", Instance, EFDebug.ScriptType.AvatarSystem);
                    return;
                }
                if (playerRigRootToStore.transform.Find(Config_AvatarStructure.RightWrist) == null)
                {
                    EFDebug.LogError("Provided player rig must have a child transform named '" + Config_AvatarStructure.RightWrist + "'.", Instance, EFDebug.ScriptType.AvatarSystem);
                    return;
                }
                if (playerRigRootToStore.transform.Find(Config_AvatarStructure.AvatarContainer) == null)
                {
                    EFDebug.LogError("Provided player rig must have a child transform named '" + Config_AvatarStructure.AvatarContainer + "'.", Instance, EFDebug.ScriptType.AvatarSystem);
                    return;
                }
                if (playerAvatarToStore == null)
                {
                    EFDebug.LogError("Provided avatar is 'null'.", Instance, EFDebug.ScriptType.EFCore);
                    return;
                }
                if (playerAvatarToStore.GetComponent<AvatarAssembler>() == null)
                {
                    EFDebug.LogError("Provided avatar does not have 'AvatarAssembler' script on it.", Instance, EFDebug.ScriptType.EFCore);
                    return;
                }
            }

            /// Store the new rig.
            LocalPlayerRig = new PlayerRigReferences()
            {
                rig_root = playerRigRootToStore,
                rig_Head = playerRigRootToStore.transform.Find(Config_AvatarStructure.Head).gameObject,
                rig_LeftWrist = playerRigRootToStore.transform.Find(Config_AvatarStructure.LeftWrist).gameObject,
                rig_RightWrist = playerRigRootToStore.transform.Find(Config_AvatarStructure.RightWrist).gameObject,
                rig_AvatarContainer = playerRigRootToStore.transform.Find(Config_AvatarStructure.AvatarContainer).gameObject,
                playerEntity = playerRigRootToStore.GetComponent<PlayerEntity>(),
                avatar = playerAvatarToStore,
                avatar_Assembler = playerAvatarToStore.GetComponent<AvatarAssembler>()
            };
        }

        /*
        /// <summary>
        /// Finds all spawn points in the active scene.
        /// </summary>
        private static SoyVRTK_SpawnPoint[] GetSpawnPoints()
        {
            return FindObjectsOfType<SoyVRTK_SpawnPoint>();
        }

        /// <summary>
        /// Finds and removes all other local player's avatars from the scene, if such exist.
        /// </summary>
        private static void RemoveDuplicateAvatars()
        {
            PlayerEntity[] sceneAvatars = FindObjectsOfType<PlayerEntity>();

            foreach (PlayerEntity avatar in sceneAvatars)
            {
                if (MultiplayerManager.IsLocal(avatar.photonView)) MultiplayerManager.NetworkSafeDestroy(avatar.gameObject);
            }
        }
        #endregion
        */
    }
}
