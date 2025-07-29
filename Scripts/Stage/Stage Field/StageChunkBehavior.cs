using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    public class StageChunkBehavior : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        public Vector2 Size => sprite.size;
        public bool IsVisible => sprite.isVisible;

        public float LeftBound => transform.position.x - Size.x / 2;
        public float RightBound => transform.position.x + Size.x / 2;
        public float TopBound => transform.position.y + Size.y / 2;
        public float BottomBound => transform.position.y - Size.y / 2;

        public bool HasEmptyLeft => LeftBound > CameraManager.LeftBound;
        public bool HasEmptyRight => RightBound < CameraManager.RightBound;
        public bool HasEmptyTop => TopBound < CameraManager.TopBound;
        public bool HasEmptyBottom => BottomBound > CameraManager.BottomBound;

        private List<Transform> borders = new List<Transform>();
        private List<PropBehavior> prop = new List<PropBehavior>();    

        public void AddBorder(Transform border)
        {
            borders.Add(border);
        }

        public void AddProp(PropBehavior propObject)
        {
            prop.Add(propObject);

            propObject.transform.position = new Vector3(Random.Range(LeftBound, RightBound), Random.Range(BottomBound, TopBound), 0);
        }

        public void RemovePropFromBossFence(BossFenceBehavior fence)
        {
            for(int i = 0; i < prop.Count; i++)
            {
                if (fence.ValidatePosition(prop[i].transform.position, Vector2.zero))
                {
                    prop[i].Dissolve();
                    prop.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Clear()
        {
            for(int i = 0; i < borders.Count; i++)
            {
                borders[i].gameObject.SetActive(false);
            }

            for(int i = 0; i < prop.Count; i++)
            {
                prop[i].gameObject.SetActive(false);
            }

            borders.Clear();
            prop.Clear();

            gameObject.SetActive(false);
        }
    }
}