using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    public class VerticalFieldBehavior : AbstractFieldBehavior
    {
        protected List<PoolComponent<StageChunkBehavior>> backgroundPools;
        protected PoolComponent<Transform> leftPool;
        protected PoolComponent<Transform> rightPool;

        protected List<StageChunkBehavior> chunks = new List<StageChunkBehavior>();

        protected bool wait = false;

        public override void Init(StageFieldData stageFieldData, bool spawnProp)
        {
            base.Init(stageFieldData, spawnProp);

            foreach (var background in stageFieldData.GetBackgroundPrefabs())
            {
                var pool = new PoolComponent<StageChunkBehavior>(background, 6);
            }

            backgroundPools = new List<PoolComponent<StageChunkBehavior>>();
            foreach (var background in stageFieldData.GetBackgroundPrefabs())
            {
                var pool = new PoolComponent<StageChunkBehavior>(background, 3);
                backgroundPools.Add(pool);
            }

            if(stageFieldData.LeftPrefab != null )
            {
                leftPool = new PoolComponent<Transform>("Field Left Border", stageFieldData.LeftPrefab, 4);
            }

            if (stageFieldData.RightPrefab != null)
            {
                rightPool = new PoolComponent<Transform>("Field Right Border", stageFieldData.RightPrefab, 4);
            }

            var chunk = backgroundPools.Random().GetEntity();

            chunk.transform.position = Vector3.zero;
            chunk.transform.rotation = Quaternion.identity;
            chunk.transform.localScale = Vector3.one;

            AddBordersToChunk(chunk);

            chunks.Add(chunk);

            wait = false;
            EasingManager.DoNextFrame().SetOnFinish(() => wait = true);
        }

        public override void Update()
        {
            if (!wait) return;

            RemoveInvisibleChunks();
            CheckForNewChunks();
        }

        #region Add New Chunks

        protected virtual void CheckForNewChunks()
        {
            TryAddTopRow();
            TryAddBottomRow();
        }

        protected virtual void TryAddTopRow()
        {
            if (chunks[0].HasEmptyTop)
            {
                var chunk = backgroundPools.Random().GetEntity();
                var chunkBellow = chunks[0];

                chunk.transform.position = chunkBellow.transform.position + Vector3.up * chunk.Size.y;
                chunk.transform.rotation = Quaternion.identity;
                chunk.transform.localScale = Vector3.one;

                AddBordersToChunk(chunk);
                SpawnProp(chunk);

                chunks.Insert(0, chunk);
            }
        }

        protected virtual void TryAddBottomRow()
        {
            if (chunks[^1].HasEmptyBottom)
            {
                var chunk = backgroundPools.Random().GetEntity();
                var chunkOnTop = chunks[^1];

                chunk.transform.position = chunkOnTop.transform.position + Vector3.down * chunk.Size.y;
                chunk.transform.rotation = Quaternion.identity;
                chunk.transform.localScale = Vector3.one;

                AddBordersToChunk(chunk);
                SpawnProp(chunk);

                chunks.Add(chunk);
            }
        }

        protected virtual void AddBordersToChunk(StageChunkBehavior chunk)
        {
            if (leftPool != null)
            {
                var leftBorder = leftPool.GetEntity();
                leftBorder.transform.position = chunk.transform.position + Vector3.left * chunk.Size.x;

                chunk.AddBorder(leftBorder);
            }

            if (rightPool != null)
            {
                var rightBorder = rightPool.GetEntity();
                rightBorder.transform.position = chunk.transform.position + Vector3.right * chunk.Size.x;

                chunk.AddBorder(rightBorder);
            }
        }

        #endregion

        #region Remove Invisible Chunks

        protected virtual void RemoveInvisibleChunks()
        {
            if (chunks.Count == 0) return;

            CheckTopRow();
            CheckBottomRow();
        }

        protected virtual void CheckTopRow()
        {
            if (!chunks[0].IsVisible)
            {
                RemoveTopRow(); 
            }
        }

        protected virtual void CheckBottomRow()
        {
            if (!chunks[^1].IsVisible)
            {
                RemoveBottomRow();
            }
        }

        protected virtual void RemoveTopRow()
        {
            chunks[0].Clear();
            chunks.RemoveAt(0);
        }

        protected virtual void RemoveBottomRow()
        {
            chunks[^1].Clear();
            chunks.RemoveAt(chunks.Count - 1);
        }

        #endregion


        public override bool ValidatePosition(Vector2 position)
        {
            if (position.x > chunks[0].transform.position.x + chunks[0].Size.x / 2) return false;
            if (position.x < chunks[0].transform.position.x - chunks[0].Size.x / 2) return false;

            return true;
        }

        public override Vector2 GetBossSpawnPosition(BossFenceBehavior fence, Vector2 offset)
        {
            var playerPosition = PlayerBehavior.Player.transform.position.XY();

            if (fence is CircleFenceBehavior circleFence)
            {
                if (circleFence.Radius < chunks[0].Size.x / 2f)
                {
                    circleFence.SetRadiusOverride(chunks[0].Size.x / 2f * 1.1f);
                }

                var positionWithOffset = new Vector2(0, playerPosition.y + offset.y);
                if (Vector3.Distance(positionWithOffset, playerPosition) < circleFence.Radius)
                {
                    return positionWithOffset;
                }
                else
                {
                    return new Vector2(0, playerPosition.y);
                }
            }
            else if (fence is RectFenceBehavior rectFence)
            {
                if (rectFence.Width < chunks[0].Size.x)
                {
                    rectFence.SetSizeOverride(chunks[0].Size.x * 1.1f, rectFence.Height);
                }

                return new Vector2(0, playerPosition.y + offset.y);
            }

            return playerPosition + offset;
        }

        public override Vector2 GetRandomPositionOnBorder()
        {
            float x = Random.Range(-chunks[0].Size.x / 2, chunks[0].Size.x / 2) + chunks[0].transform.position.x;

            float sign = Random.Range(0, 2) * 2 - 1;
            float y = PlayerBehavior.Player.transform.position.y + CameraManager.HalfHeight * 1.05f * sign;

            return new Vector2(x, y);
        }

        public override bool IsPointOutsideRight(Vector2 point, out float distance)
        {
            bool result = point.x > chunks[0].RightBound;
            distance = result ? point.x - chunks[0].RightBound : 0;
            return result;
        }

        public override bool IsPointOutsideLeft(Vector2 point, out float distance)
        {
            bool result = point.x < chunks[0].LeftBound;
            distance = result ? chunks[0].LeftBound - point.x : 0;
            return result;
        }

        public override bool IsPointOutsideTop(Vector2 point, out float distance)
        {
            distance = 0;
            return false;
        }

        public override bool IsPointOutsideBottom(Vector2 point, out float distance)
        {
            distance = 0;
            return false;
        }

        public override void RemovePropFromBossFence(BossFenceBehavior fence)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].RemovePropFromBossFence(fence);
            }
        }

        public override void Clear()
        {
            for(int i = 0; i < backgroundPools.Count; i++)
            {
                backgroundPools[i].Destroy();
            }

            backgroundPools.Clear();
        }
    }
}