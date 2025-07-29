using UnityEngine;
using System.Collections.Generic;

namespace Talents.Config
{
    [CreateAssetMenu(fileName = "TalentLayoutConfig", menuName = "Talents/Layout Config")]
    public class TalentLayoutConfig : ScriptableObject
    {
        [Header("Zone Layout Settings")]
        [SerializeField] private float nodeSpacing = 280f;
        [SerializeField] private float zoneSpacing = 120f;
        [SerializeField] private float columnDistance = 300f;
        [SerializeField] private float startY = 150f;
        
        [Header("Column Positions")]
        [SerializeField] private float normalColumnX = -150f;
        [SerializeField] private float specialColumnX = 150f;
        
        [Header("Node Size Settings")]
        [SerializeField] private Vector2 normalNodeSize = new Vector2(120f, 120f);
        [SerializeField] private Vector2 specialNodeSize = new Vector2(140f, 140f);
        [SerializeField] private Vector2 zoneLabelSize = new Vector2(250f, 60f);
        
        [Header("Zone Label Settings")]
        [SerializeField] private float zoneLabelOffsetY = -80f;
        [SerializeField] private bool showZoneLabels = true;
        [SerializeField] private Color zoneLabelColor = Color.yellow;
        [SerializeField] private int zoneLabelFontSize = 28;
        
        [Header("Connection Settings")]
        [SerializeField] private float connectionLineWidth = 4f;
        [SerializeField] private Color activeConnectionColor = Color.green;
        [SerializeField] private Color inactiveConnectionColor = Color.gray;
        [SerializeField] private bool showConnections = true;
        
        [Header("Level Configuration")]
        [SerializeField] private int maxPlayerLevel = 30;
        [SerializeField] private bool autoGenerateNormalNodes = true;
        [SerializeField] private List<ZoneConfig> zoneConfigs = new List<ZoneConfig>();
        
        [Header("Normal Stats Generation")]
        [SerializeField] private float statGrowthRate = 1.2f;
        [SerializeField] private float costGrowthRate = 1.15f;
        
        [Header("Icon Settings")]
        [SerializeField] private string iconBasePath = "Icons/Talents/";
        [SerializeField] private string defaultNormalIcon = "default_normal";
        [SerializeField] private string defaultSpecialIcon = "default_special";
        [SerializeField] private Vector2 iconSize = new Vector2(80f, 80f);
        
        [Header("Mobile Optimization")]
        [SerializeField] private bool mobileOptimized = true;
        [SerializeField] private float mobileScaleFactor = 1f;
        [SerializeField] private int nodesPerScreen = 4;

        // Properties
        public float NodeSpacing => nodeSpacing * (mobileOptimized ? mobileScaleFactor : 1f);
        public float ZoneSpacing => zoneSpacing * (mobileOptimized ? mobileScaleFactor : 1f);
        public float ColumnDistance => columnDistance * (mobileOptimized ? mobileScaleFactor : 1f);
        public float StartY => startY;
        public float NormalColumnX => normalColumnX;
        public float SpecialColumnX => specialColumnX;
        public Vector2 NormalNodeSize => normalNodeSize * (mobileOptimized ? mobileScaleFactor : 1f);
        public Vector2 SpecialNodeSize => specialNodeSize * (mobileOptimized ? mobileScaleFactor : 1f);
        public Vector2 ZoneLabelSize => zoneLabelSize * (mobileOptimized ? mobileScaleFactor : 1f);
        public float ZoneLabelOffsetY => zoneLabelOffsetY;
        public bool ShowZoneLabels => showZoneLabels;
        public Color ZoneLabelColor => zoneLabelColor;
        public int ZoneLabelFontSize => Mathf.RoundToInt(zoneLabelFontSize * (mobileOptimized ? mobileScaleFactor : 1f));
        public float ConnectionLineWidth => connectionLineWidth;
        public Color ActiveConnectionColor => activeConnectionColor;
        public Color InactiveConnectionColor => inactiveConnectionColor;
        public bool ShowConnections => showConnections;
        public int MaxPlayerLevel => maxPlayerLevel;
        public bool AutoGenerateNormalNodes => autoGenerateNormalNodes;
        public float StatGrowthRate => statGrowthRate;
        public float CostGrowthRate => costGrowthRate;
        public string IconBasePath => iconBasePath;
        public string DefaultNormalIcon => defaultNormalIcon;
        public string DefaultSpecialIcon => defaultSpecialIcon;
        public Vector2 IconSize => iconSize * (mobileOptimized ? mobileScaleFactor : 1f);
        public int NodesPerScreen => nodesPerScreen;

        /// <summary>
        /// Get zone config for specific level
        /// </summary>
        public ZoneConfig GetZoneConfig(int zoneLevel)
        {
            var config = zoneConfigs.Find(z => z.zoneLevel == zoneLevel);
            if (config != null) return config;
            
            // Return default config if not found
            return new ZoneConfig
            {
                zoneLevel = zoneLevel,
                hasSpecialNode = false,
                normalNodeCount = 4,
                customSpacing = 0f
            };
        }

        /// <summary>
        /// Calculate total content height
        /// </summary>
        public float CalculateTotalContentHeight()
        {
            float totalHeight = StartY;
            
            for (int zone = 1; zone <= MaxPlayerLevel; zone++)
            {
                var zoneConfig = GetZoneConfig(zone);
                float zoneHeight = NodeSpacing * zoneConfig.normalNodeCount;
                
                if (zoneConfig.customSpacing > 0)
                    zoneHeight += zoneConfig.customSpacing;
                else
                    zoneHeight += ZoneSpacing;
                
                totalHeight += zoneHeight;
            }
            
            return totalHeight + 200f; // Extra padding
        }

        /// <summary>
        /// Calculate node position for zone
        /// </summary>
        public Vector2 CalculateNodePosition(int zoneLevel, int nodeIndex, bool isSpecial = false)
        {
            float x = isSpecial ? SpecialColumnX : NormalColumnX;
            float y = CalculateZoneStartY(zoneLevel) + (nodeIndex * NodeSpacing);
            
            return new Vector2(x, y);
        }

        /// <summary>
        /// Calculate zone start Y position
        /// </summary>
        public float CalculateZoneStartY(int zoneLevel)
        {
            float currentY = StartY;
            
            for (int zone = 1; zone < zoneLevel; zone++)
            {
                var zoneConfig = GetZoneConfig(zone);
                float zoneHeight = NodeSpacing * zoneConfig.normalNodeCount;
                
                if (zoneConfig.customSpacing > 0)
                    zoneHeight += zoneConfig.customSpacing;
                else
                    zoneHeight += ZoneSpacing;
                
                currentY += zoneHeight;
            }
            
            return currentY;
        }

        /// <summary>
        /// Calculate zone label position
        /// </summary>
        public Vector2 CalculateZoneLabelPosition(int zoneLevel)
        {
            float zoneStartY = CalculateZoneStartY(zoneLevel);
            float zoneCenterY = zoneStartY + (NodeSpacing * 2f); // Center of 4 nodes
            
            return new Vector2(0f, zoneCenterY + ZoneLabelOffsetY);
        }

        /// <summary>
        /// Get optimal content size for mobile
        /// </summary>
        public Vector2 GetOptimalContentSize()
        {
            float width = Mathf.Abs(NormalColumnX) + Mathf.Abs(SpecialColumnX) + 
                         Mathf.Max(NormalNodeSize.x, SpecialNodeSize.x) + 100f;
            float height = CalculateTotalContentHeight();
            
            return new Vector2(width, height);
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool ValidateConfig()
        {
            bool isValid = true;
            
            if (nodeSpacing <= 0)
            {
                Debug.LogError("[TalentLayoutConfig] Node spacing must be greater than 0");
                isValid = false;
            }
            
            if (MaxPlayerLevel <= 0)
            {
                Debug.LogError("[TalentLayoutConfig] Max zones must be greater than 0");
                isValid = false;
            }
            
            if (normalNodeSize.x <= 0 || normalNodeSize.y <= 0)
            {
                Debug.LogError("[TalentLayoutConfig] Normal node size must be greater than 0");
                isValid = false;
            }
            
            return isValid;
        }

        /// <summary>
        /// Apply mobile optimization
        /// </summary>
        public void ApplyMobileOptimization(Vector2 screenSize)
        {
            // Calculate scale factor based on screen height
            float targetNodesPerScreen = 4f;
            float availableHeight = screenSize.y * 0.8f; // 80% of screen for content
            float requiredSpacing = availableHeight / targetNodesPerScreen;
            
            if (requiredSpacing < nodeSpacing)
            {
                mobileScaleFactor = requiredSpacing / nodeSpacing;
                mobileOptimized = true;
            }
            else
            {
                mobileScaleFactor = 1f;
                mobileOptimized = false;
            }
        }

        /// <summary>
        /// Reset to default values optimized for auto-generation
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            // Uniform spacing for all nodes
            nodeSpacing = 280f;           // Consistent spacing between ALL nodes
            zoneSpacing = 0f;             // No extra zone spacing - uniform throughout
            columnDistance = 320f;        // Column separation
            startY = 50f;                 // Start very close to bottom
            
            // Column positions
            normalColumnX = -160f;        
            specialColumnX = 160f;        
            
            // Node sizes
            normalNodeSize = new Vector2(120f, 120f);    
            specialNodeSize = new Vector2(140f, 140f);   
            zoneLabelSize = new Vector2(280f, 60f);      // Smaller labels
            
            // Zone label positioning - minimal offset
            zoneLabelOffsetY = 10f;       // Very close to nodes
            showZoneLabels = true;
            zoneLabelColor = new Color(1f, 0.9f, 0.3f); 
            zoneLabelFontSize = 24;       // Smaller font
            
            // Connection settings
            connectionLineWidth = 4f;     
            activeConnectionColor = new Color(0.2f, 0.9f, 0.2f, 0.9f);   
            inactiveConnectionColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); 
            showConnections = true;
            
            // Level configuration
            maxPlayerLevel = 30;          // 30 levels by default
            autoGenerateNormalNodes = true;
            statGrowthRate = 1.2f;        // 20% growth per level
            costGrowthRate = 1.15f;       // 15% cost increase per level
            
            // Icon settings
            iconBasePath = "Icons/Talents/";
            defaultNormalIcon = "default_normal";
            defaultSpecialIcon = "default_special";
            iconSize = new Vector2(80f, 80f);  
            
            // Mobile optimization
            mobileOptimized = true;
            mobileScaleFactor = 1f;
            nodesPerScreen = 5;           // Target 5 nodes per screen with uniform spacing
        }

        /// <summary>
        /// Create zone configs from CSV data
        /// </summary>
        public void GenerateZoneConfigsFromCSV(List<Talents.Data.TalentModel> talents)
        {
            zoneConfigs.Clear();
            
            var zoneGroups = new Dictionary<int, List<Talents.Data.TalentModel>>();
            
            // Group talents by required player level
            foreach (var talent in talents)
            {
                int zone = talent.RequiredPlayerLevel;
                if (!zoneGroups.ContainsKey(zone))
                    zoneGroups[zone] = new List<Talents.Data.TalentModel>();
                
                zoneGroups[zone].Add(talent);
            }
            
            // Create zone configs
            foreach (var zoneGroup in zoneGroups)
            {
                int zoneLevel = zoneGroup.Key;
                var zoneTalents = zoneGroup.Value;
                
                int normalCount = 0;
                bool hasSpecial = false;
                
                foreach (var talent in zoneTalents)
                {
                    if (talent.NodeType == Talents.Data.TalentNodeType.Normal)
                        normalCount++;
                    else if (talent.NodeType == Talents.Data.TalentNodeType.Special)
                        hasSpecial = true;
                }
                
                var zoneConfig = new ZoneConfig
                {
                    zoneLevel = zoneLevel,
                    normalNodeCount = Mathf.Max(normalCount, 4), // Minimum 4 normal nodes
                    hasSpecialNode = hasSpecial,
                    customSpacing = 0f
                };
                
                zoneConfigs.Add(zoneConfig);
            }
        }
    }

    /// <summary>
    /// Configuration for individual zones
    /// </summary>
    [System.Serializable]
    public class ZoneConfig
    {
        [SerializeField] public int zoneLevel;
        [SerializeField] public int normalNodeCount = 4;
        [SerializeField] public bool hasSpecialNode = false;
        [SerializeField] public float customSpacing = 0f;
        [SerializeField] public bool useCustomLayout = false;
        [SerializeField] public Vector2 customNormalPosition = Vector2.zero;
        [SerializeField] public Vector2 customSpecialPosition = Vector2.zero;
        
        /// <summary>
        /// Get zone height including spacing
        /// </summary>
        public float GetZoneHeight(float nodeSpacing, float defaultZoneSpacing)
        {
            float height = nodeSpacing * normalNodeCount;
            height += customSpacing > 0 ? customSpacing : defaultZoneSpacing;
            return height;
        }
    }
}