using UnityEngine;

namespace OctoberStudio
{
    public class RectFenceBehavior : BossFenceBehavior
    {
        [SerializeField] float height;
        [SerializeField] float width;

        public float Height { get; private set; }
        public float Width { get; private set; }

        private int horyzontalCount;
        private int verticalCount;

        private float halfHeight;
        private float halfWidth;

        public override void Init()
        {
            Height = height;
            Width = width;

            horyzontalCount = Mathf.RoundToInt(Width / linkDistance);
            verticalCount = Mathf.RoundToInt(Height / linkDistance);

            halfHeight = Height / 2;
            halfWidth = Width / 2;

            FencePoolSize = verticalCount * 2 + horyzontalCount * 2;

            base.Init();
        }

        public override void SpawnFence(Vector2 center)
        {
            base.SpawnFence(center);

            for(int i = 0 ; i < horyzontalCount; i++)
            {
                var x = -halfWidth + i * linkDistance;

                var topPosition = center + new Vector2(x, halfHeight);
                var bottomPosition = center + new Vector2(x, -halfHeight);

                if (StageController.FieldManager.ValidatePosition(topPosition, Vector2.zero, false))
                {
                    var link = fenceLinkPool.GetEntity();
                    link.transform.position = topPosition;
                    link.transform.rotation = Quaternion.identity;

                    fenceLinks.Add(link);
                }

                if (StageController.FieldManager.ValidatePosition(bottomPosition, Vector2.zero, false))
                {
                    var link = fenceLinkPool.GetEntity();
                    link.transform.position = bottomPosition;
                    link.transform.rotation = Quaternion.identity;

                    fenceLinks.Add(link);
                }
            }

            for (int i = 0; i < verticalCount; i++)
            {
                var y = -halfHeight + i * linkDistance;

                var leftPosition = center + new Vector2(-halfWidth, y);
                var rightposition = center + new Vector2(halfWidth, y);

                if (StageController.FieldManager.ValidatePosition(leftPosition, Vector2.zero, false))
                {
                    var link = fenceLinkPool.GetEntity();
                    link.transform.position = leftPosition;
                    link.transform.rotation = Quaternion.identity;

                    fenceLinks.Add(link);
                }

                if (StageController.FieldManager.ValidatePosition(rightposition, Vector2.zero, false))
                {
                    var link = fenceLinkPool.GetEntity();
                    link.transform.position = rightposition;
                    link.transform.rotation = Quaternion.identity;

                    fenceLinks.Add(link);
                }
            }
        }

        public override bool ValidatePosition(Vector2 position, Vector2 offset)
        {
            float halfWidthOffset = halfWidth - offset.x;
            float halfHeightOffset = halfHeight - offset.y;

            if (position.x > Center.x + halfWidthOffset) return false;
            if (position.x < Center.x - halfWidthOffset) return false;

            if (position.y > Center.y + halfHeightOffset) return false;
            if (position.y < Center.y - halfHeightOffset) return false;

            return true;
        }

        public override Vector2 GetRandomPointInside(float offset)
        {
            float halfWidthOffset = halfWidth - offset;
            float halfHeightOffset = halfHeight - offset;

            return Center + new Vector2(Random.Range(-1f, 1f) * halfWidthOffset, Random.Range(-1f, 1f) * halfHeightOffset);
        }

        public void SetSizeOverride(float widthOverride, float heightOverride)
        {
            Height = heightOverride;
            Width = widthOverride;

            horyzontalCount = Mathf.RoundToInt(Width / linkDistance);
            verticalCount = Mathf.RoundToInt(Height / linkDistance);

            halfHeight = Height / 2;
            halfWidth = Width / 2;
        }

        public void ResetSizeOverride()
        {
            Height = height;
            Width = width;

            horyzontalCount = Mathf.RoundToInt(Width / linkDistance);
            verticalCount = Mathf.RoundToInt(Height / linkDistance);

            halfHeight = Height / 2;
            halfWidth = Width / 2;
        }

        public override void RemoveFence()
        {
            base.RemoveFence();

            ResetSizeOverride();
        }
    }
}