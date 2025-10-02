using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Abilities
{
    public class SwordSlashBehavior : ProjectileBehavior
    {
        [SerializeField] Collider2D slashCollider;
        [SerializeField] SpriteRenderer[] spriteRenderers;

        public float Size { get; set; }
        public float Range { get; set; }
        public float Speed { get; set; }
        public Vector2 Direction { get; set; }
        public float SlashAnimationDuration { get; set; }
        public float MoveDuration { get; set; }

        public UnityAction<SwordSlashBehavior> onFinished;

        private IEasingCoroutine animationCoroutine;
        private IEasingCoroutine colliderCoroutine;

        private Vector3 originalColliderScale;
        private Color[] originalColors;
        
        private bool isSlashAnimationComplete = false;
        private float moveStartTime;

        private void Awake()
        {
            // Store original collider scale/size
            if (slashCollider is CircleCollider2D circleCollider)
            {
                originalColliderScale = new Vector3(circleCollider.radius, circleCollider.radius, 1f);
            }
            else if (slashCollider is BoxCollider2D boxCollider)
            {
                originalColliderScale = boxCollider.size;
            }
            else
            {
                originalColliderScale = Vector3.one;
            }

            // Store original colors
            if (spriteRenderers != null && spriteRenderers.Length > 0)
            {
                originalColors = new Color[spriteRenderers.Length];
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        originalColors[i] = spriteRenderers[i].color;
                    }
                }
            }
        }

        public override void Init()
        {
            base.Init();

            isSlashAnimationComplete = false;

            // Apply visual size
            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;

            // Apply collider range
            ApplyColliderRange();

            // Ẩn tất cả sprites ban đầu
            HideAllSprites();

            slashCollider.enabled = true;

            // Bật collider trong thời gian animation chém
            colliderCoroutine = EasingManager.DoAfter(SlashAnimationDuration, () => slashCollider.enabled = false);

            // Bắt đầu animation chém
            StartSlashAnimation();
        }

        private void StartSlashAnimation()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0) return;

            float timePerSprite = SlashAnimationDuration / spriteRenderers.Length;

            // Hiển thị từng sprite theo thứ tự
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                int index = i;
                EasingManager.DoAfter(timePerSprite * i, () => 
                {
                    ShowSpriteAtIndex(index);
                    
                    // Nếu là sprite cuối cùng, bắt đầu phase di chuyển
                    if (index == spriteRenderers.Length - 1)
                    {
                        isSlashAnimationComplete = true;
                        moveStartTime = Time.time;
                    }
                });
            }

            // Kết thúc sau khi animation + di chuyển xong
            animationCoroutine = EasingManager.DoAfter(SlashAnimationDuration + MoveDuration, () => 
            {
                onFinished?.Invoke(this);
                Disable();
            });
        }

        private void Update()
        {
            if (gameObject.activeSelf && isSlashAnimationComplete)
            {
                // Di chuyển sprite cuối cùng
                transform.position += (Vector3)Direction * Speed * Time.deltaTime;

                // Fade out sprite cuối cùng
                float moveElapsed = Time.time - moveStartTime;
                float alpha = 1f - (moveElapsed / MoveDuration);
                SetAlphaForLastSprite(Mathf.Clamp01(alpha));
            }
        }

        private void HideAllSprites()
        {
            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        spriteRenderers[i].enabled = false;
                    }
                }
            }
        }

        private void ShowSpriteAtIndex(int index)
        {
            if (spriteRenderers == null || index < 0 || index >= spriteRenderers.Length) return;

            // Ẩn tất cả sprites trước đó
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].enabled = (i == index);
                    
                    // Reset alpha cho sprite hiện tại
                    if (i == index && i < originalColors.Length)
                    {
                        Color color = originalColors[i];
                        color.a = 1f;
                        spriteRenderers[i].color = color;
                    }
                }
            }
        }

        private void SetAlphaForLastSprite(float alpha)
        {
            if (spriteRenderers != null && spriteRenderers.Length > 0)
            {
                int lastIndex = spriteRenderers.Length - 1;
                if (spriteRenderers[lastIndex] != null && lastIndex < originalColors.Length)
                {
                    Color color = originalColors[lastIndex];
                    color.a = alpha;
                    spriteRenderers[lastIndex].color = color;
                }
            }
        }

        private void ApplyColliderRange()
        {
            float rangeMultiplier = Range * PlayerBehavior.Player.SizeMultiplier;

            if (slashCollider is CircleCollider2D circleCollider)
            {
                circleCollider.radius = originalColliderScale.x * rangeMultiplier;
            }
            else if (slashCollider is BoxCollider2D boxCollider)
            {
                boxCollider.size = new Vector2(
                    originalColliderScale.x * rangeMultiplier,
                    originalColliderScale.y * rangeMultiplier
                );
            }
            else if (slashCollider is CapsuleCollider2D capsuleCollider)
            {
                capsuleCollider.size = new Vector2(
                    originalColliderScale.x * rangeMultiplier,
                    originalColliderScale.y * rangeMultiplier
                );
            }
        }

        public void Disable()
        {
            animationCoroutine.StopIfExists();
            colliderCoroutine.StopIfExists();

            gameObject.SetActive(false);
            slashCollider.enabled = true;
            HideAllSprites();
            isSlashAnimationComplete = false;
        }
    }
}