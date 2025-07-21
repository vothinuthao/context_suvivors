using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Talents.Data;
using Talents.Manager;
using Talents.UI;

namespace Talents.Connections
{
    /// <summary>
    /// System for managing connection lines between talent nodes
    /// </summary>
    public class TalentConnectionSystem : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private Transform connectionParent;
        [SerializeField] private float lineWidth = 2f;
        [SerializeField] private Material connectionMaterial;

        [Header("Visual States")]
        [SerializeField] private Color activeConnectionColor = Color.green;
        [SerializeField] private Color availableConnectionColor = Color.white;
        [SerializeField] private Color lockedConnectionColor = Color.gray;
        [SerializeField] private Gradient connectionGradient;

        [Header("Animation")]
        [SerializeField] private bool animateConnections = true;
        [SerializeField] private float animationSpeed = 2f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Connection management
        private Dictionary<int, List<TalentConnection>> talentConnections = new Dictionary<int, List<TalentConnection>>();
        private List<TalentConnection> allConnections = new List<TalentConnection>();
        private Dictionary<int, TalentNodeBehavior> nodeMap = new Dictionary<int, TalentNodeBehavior>();

        // Object pooling
        private Queue<TalentConnection> connectionPool = new Queue<TalentConnection>();
        private int initialPoolSize = 50;

        private void Start()
        {
            InitializeConnectionPool();
            
            if (connectionParent == null)
            {
                connectionParent = transform;
            }
        }

        /// <summary>
        /// Initialize connection object pool
        /// </summary>
        private void InitializeConnectionPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var connection = CreateConnection();
                connection.gameObject.SetActive(false);
                connectionPool.Enqueue(connection);
            }
        }

        /// <summary>
        /// Create a new connection object
        /// </summary>
        private TalentConnection CreateConnection()
        {
            GameObject connectionObj;
            
            if (connectionLinePrefab != null)
            {
                connectionObj = Instantiate(connectionLinePrefab, connectionParent);
            }
            else
            {
                connectionObj = new GameObject("TalentConnection");
                connectionObj.transform.SetParent(connectionParent);
            }

            var connection = connectionObj.GetComponent<TalentConnection>();
            if (connection == null)
            {
                connection = connectionObj.AddComponent<TalentConnection>();
            }

            connection.Initialize(lineWidth, connectionMaterial);
            return connection;
        }

        /// <summary>
        /// Get connection from pool
        /// </summary>
        private TalentConnection GetPooledConnection()
        {
            if (connectionPool.Count > 0)
            {
                var connection = connectionPool.Dequeue();
                connection.gameObject.SetActive(true);
                return connection;
            }
            else
            {
                return CreateConnection();
            }
        }

        /// <summary>
        /// Return connection to pool
        /// </summary>
        private void ReturnConnectionToPool(TalentConnection connection)
        {
            connection.gameObject.SetActive(false);
            connectionPool.Enqueue(connection);
        }

        /// <summary>
        /// Build connections for all talents
        /// </summary>
        public void BuildConnections(Dictionary<int, TalentNodeBehavior> nodes)
        {
            nodeMap = nodes;
            ClearAllConnections();

            if (!TalentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogWarning("[TalentConnectionSystem] Database not loaded");
                return;
            }

            var allTalents = TalentDatabase.Instance.GetAllTalents();
            
            foreach (var talent in allTalents)
            {
                if (talent.HasPrerequisite && nodeMap.ContainsKey(talent.ID) && nodeMap.ContainsKey(talent.RequiredTalentId))
                {
                    CreateConnection(talent.RequiredTalentId, talent.ID);
                }
            }

            UpdateAllConnectionStates();
        }

        /// <summary>
        /// Create connection between two talents
        /// </summary>
        private void CreateConnection(int fromTalentId, int toTalentId)
        {
            if (!nodeMap.ContainsKey(fromTalentId) || !nodeMap.ContainsKey(toTalentId))
            {
                Debug.LogWarning($"[TalentConnectionSystem] Cannot create connection: nodes not found ({fromTalentId} -> {toTalentId})");
                return;
            }

            var fromNode = nodeMap[fromTalentId];
            var toNode = nodeMap[toTalentId];

            var connection = GetPooledConnection();
            connection.SetupConnection(fromNode, toNode, fromTalentId, toTalentId);

            // Add to tracking lists
            if (!talentConnections.ContainsKey(fromTalentId))
            {
                talentConnections[fromTalentId] = new List<TalentConnection>();
            }
            talentConnections[fromTalentId].Add(connection);
            allConnections.Add(connection);

            // Set initial state
            UpdateConnectionState(connection);
        }

        /// <summary>
        /// Update state of a specific connection
        /// </summary>
        private void UpdateConnectionState(TalentConnection connection)
        {
            if (TalentManager.Instance == null)
                return;

            var fromTalentLearned = TalentManager.Instance.IsTalentLearned(connection.FromTalentId);
            var toTalentLearned = TalentManager.Instance.IsTalentLearned(connection.ToTalentId);
            var toTalentAvailable = TalentManager.Instance.CanLearnTalent(connection.ToTalentId);

            ConnectionState newState;
            Color connectionColor;

            if (fromTalentLearned && toTalentLearned)
            {
                newState = ConnectionState.Active;
                connectionColor = activeConnectionColor;
            }
            else if (fromTalentLearned && toTalentAvailable)
            {
                newState = ConnectionState.Available;
                connectionColor = availableConnectionColor;
            }
            else
            {
                newState = ConnectionState.Locked;
                connectionColor = lockedConnectionColor;
            }

            connection.SetState(newState, connectionColor, animateConnections);
        }

        /// <summary>
        /// Update all connection states
        /// </summary>
        public void UpdateAllConnectionStates()
        {
            foreach (var connection in allConnections)
            {
                UpdateConnectionState(connection);
            }
        }

        /// <summary>
        /// Update connections for specific talent
        /// </summary>
        public void UpdateConnectionsForTalent(int talentId)
        {
            // Update outgoing connections
            if (talentConnections.ContainsKey(talentId))
            {
                foreach (var connection in talentConnections[talentId])
                {
                    UpdateConnectionState(connection);
                }
            }

            // Update incoming connections
            foreach (var connection in allConnections)
            {
                if (connection.ToTalentId == talentId)
                {
                    UpdateConnectionState(connection);
                }
            }
        }

        /// <summary>
        /// Clear all connections
        /// </summary>
        public void ClearAllConnections()
        {
            foreach (var connection in allConnections)
            {
                ReturnConnectionToPool(connection);
            }

            allConnections.Clear();
            talentConnections.Clear();
        }

        /// <summary>
        /// Highlight path to talent
        /// </summary>
        public void HighlightPathToTalent(int talentId)
        {
            // Find path from root to target talent
            var path = FindPathToTalent(talentId);
            
            // Highlight connections in path
            foreach (var connection in allConnections)
            {
                bool inPath = IsConnectionInPath(connection, path);
                connection.SetHighlighted(inPath);
            }
        }

        /// <summary>
        /// Clear all highlights
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var connection in allConnections)
            {
                connection.SetHighlighted(false);
            }
        }

        /// <summary>
        /// Find path from root to target talent
        /// </summary>
        private List<int> FindPathToTalent(int targetTalentId)
        {
            var path = new List<int>();
            var currentTalentId = targetTalentId;

            while (currentTalentId != -1)
            {
                path.Insert(0, currentTalentId);
                
                var talent = TalentDatabase.Instance.GetTalentById(currentTalentId);
                if (talent == null || !talent.HasPrerequisite)
                    break;
                
                currentTalentId = talent.RequiredTalentId;
            }

            return path;
        }

        /// <summary>
        /// Check if connection is in path
        /// </summary>
        private bool IsConnectionInPath(TalentConnection connection, List<int> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (connection.FromTalentId == path[i] && connection.ToTalentId == path[i + 1])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Animate connection activation
        /// </summary>
        public void AnimateConnectionActivation(int fromTalentId, int toTalentId)
        {
            var connection = FindConnection(fromTalentId, toTalentId);
            if (connection != null)
            {
                connection.AnimateActivation();
            }
        }

        /// <summary>
        /// Find connection between two talents
        /// </summary>
        private TalentConnection FindConnection(int fromTalentId, int toTalentId)
        {
            if (talentConnections.ContainsKey(fromTalentId))
            {
                return talentConnections[fromTalentId].Find(c => c.ToTalentId == toTalentId);
            }
            return null;
        }

        /// <summary>
        /// Get connections for debugging
        /// </summary>
        public List<TalentConnection> GetAllConnections()
        {
            return new List<TalentConnection>(allConnections);
        }

        /// <summary>
        /// Get connection statistics
        /// </summary>
        public ConnectionStatistics GetStatistics()
        {
            int activeCount = 0;
            int availableCount = 0;
            int lockedCount = 0;

            foreach (var connection in allConnections)
            {
                switch (connection.CurrentState)
                {
                    case ConnectionState.Active:
                        activeCount++;
                        break;
                    case ConnectionState.Available:
                        availableCount++;
                        break;
                    case ConnectionState.Locked:
                        lockedCount++;
                        break;
                }
            }

            return new ConnectionStatistics
            {
                TotalConnections = allConnections.Count,
                ActiveConnections = activeCount,
                AvailableConnections = availableCount,
                LockedConnections = lockedCount,
                PoolSize = connectionPool.Count
            };
        }

        // Debug methods
        [ContextMenu("Update All Connections")]
        public void UpdateAllConnectionsDebug()
        {
            UpdateAllConnectionStates();
        }

        [ContextMenu("Log Connection Statistics")]
        public void LogConnectionStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"[TalentConnectionSystem] Statistics:\n" +
                     $"Total: {stats.TotalConnections}\n" +
                     $"Active: {stats.ActiveConnections}\n" +
                     $"Available: {stats.AvailableConnections}\n" +
                     $"Locked: {stats.LockedConnections}\n" +
                     $"Pool Size: {stats.PoolSize}");
        }

        [ContextMenu("Clear All Connections")]
        public void ClearAllConnectionsDebug()
        {
            ClearAllConnections();
        }

        /// <summary>
        /// Connection state enumeration
        /// </summary>
        public enum ConnectionState
        {
            Locked,
            Available,
            Active
        }

        /// <summary>
        /// Connection statistics structure
        /// </summary>
        [System.Serializable]
        public struct ConnectionStatistics
        {
            public int TotalConnections;
            public int ActiveConnections;
            public int AvailableConnections;
            public int LockedConnections;
            public int PoolSize;
        }
    }
}