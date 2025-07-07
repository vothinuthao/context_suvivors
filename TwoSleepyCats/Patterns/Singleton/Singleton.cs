// =============================================================================
// Two Sleepy Cats Studio - Singleton
// 
// Author: Two Sleepy Cats Development Team
// Version: 1.0.0 - Core Foundation
// Created: 2025
// 
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

namespace TwoSleepyCats.Patterns.Singleton
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static readonly object _lock = new object();
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            if (_instance is Singleton<T> singleton)
                            {
                                singleton.Initialize();
                            }
                        }
                    }
                }

                return _instance;
            }
        }

        protected virtual void Initialize()
        {
        }

        public virtual void Dispose()
        {
            _instance = null;
        }
    }
}