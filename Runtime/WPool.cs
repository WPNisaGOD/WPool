using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WPNisaGOD.WPool
{
    public abstract class WPoolBase
    {
        public abstract bool AutoExpand { get; set; }
        public abstract int TotalCount { get; }
        public abstract int AvailableCount { get; }

        public abstract void Clear();
        public abstract void Prune();
        public abstract bool IsLeased(MonoBehaviour element);
    }

    public class WPool<T> : WPoolBase where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Stack<T> _available = new Stack<T>();
        private readonly List<T> _allObjects = new List<T>();
        private readonly HashSet<T> _leased = new HashSet<T>();

        private bool _autoExpand = true;

        public T Prefab => _prefab;
        public override bool AutoExpand { get => _autoExpand; set => _autoExpand = value; }
        public override int TotalCount => _allObjects.Count;
        public override int AvailableCount => _available.Count;

        private WPool(T prefab, int initialCount)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            _prefab = prefab;

            for (int i = 0; i < initialCount; i++)
                CreateObject(setActive: false, addToAvailable: true);
        }

        internal static WPool<T> CreateInternal(T prefab, int initialCount)
            => new WPool<T>(prefab, initialCount);

        private T CreateObject(bool setActive, bool addToAvailable)
        {
            if (_prefab == null)
                throw new MissingReferenceException(
                    $"Префаб пула {typeof(T)} уничтожен. Пересоздайте пул через WPool.GetOrCreate.");

            T obj = Object.Instantiate(_prefab);
            if (obj == null)
                throw new MissingReferenceException(
                    $"Instantiate вернул null для префаба пула {typeof(T)}.");

            // root → child of DDOL root: остаётся в DDOL-сцене, иерархия чистая
            obj.transform.SetParent(WPool.Root, worldPositionStays: false);
            obj.gameObject.SetActive(setActive);

            _allObjects.Add(obj);
            if (addToAvailable) _available.Push(obj);
            return obj;
        }

        public T GetFreeElement(bool activate = true)
        {
            while (_available.Count > 0)
            {
                T obj = _available.Pop();

                if (obj == null)
                {
                    _allObjects.Remove(obj);
                    continue;
                }

                if (activate) obj.gameObject.SetActive(true);
                _leased.Add(obj);
                return obj;
            }

            if (_autoExpand)
            {
                T obj = CreateObject(activate, addToAvailable: false);
                _leased.Add(obj);
                return obj;
            }

            throw new InvalidOperationException(
                $"Нет свободных объектов в пуле типа {typeof(T)}. Включите AutoExpand.");
        }

        public void Release(T element)
        {
            if (element == null) return;
            if (!_leased.Remove(element)) return;
            if (element == null) return;

            // Возврат в DDOL: сначала отвязать от родителя в сцене,
            // иначе Unity оставит объект в сцене родителя.
            element.transform.SetParent(WPool.Root, worldPositionStays: true);
            element.gameObject.SetActive(false);
            _available.Push(element);
        }

        public void Expand(int additionalCount)
        {
            for (int i = 0; i < additionalCount; i++)
                CreateObject(setActive: false, addToAvailable: true);
        }

        public override bool IsLeased(MonoBehaviour element)
            => element is T t && t != null && _leased.Contains(t);

        public override void Prune()
        {
            _allObjects.RemoveAll(o => o == null);
            _leased.RemoveWhere(o => o == null);

            var tmp = _available.ToArray();
            _available.Clear();
            for (int i = tmp.Length - 1; i >= 0; i--)
                if (tmp[i] != null)
                    _available.Push(tmp[i]);
        }

        public override void Clear()
        {
            foreach (var obj in _allObjects)
                if (obj != null) Object.Destroy(obj.gameObject);

            _allObjects.Clear();
            _available.Clear();
            _leased.Clear();
        }
    }

    public static class WPool
    {
        private static readonly Dictionary<Type, Dictionary<MonoBehaviour, WPoolBase>> _pools
            = new Dictionary<Type, Dictionary<MonoBehaviour, WPoolBase>>();

        private static readonly Dictionary<MonoBehaviour, WPoolBase> _elementToPool
            = new Dictionary<MonoBehaviour, WPoolBase>();

        private static Transform _root;

        /// <summary>
        /// DDOL-root, под которым лежат все объекты пулов.
        /// </summary>
        public static Transform Root
        {
            get
            {
                if (_root == null)
                {
                    var go = new GameObject("[WPool]");
                    Object.DontDestroyOnLoad(go);
                    _root = go.transform;
                }
                return _root;
            }
        }

        public static WPool<T> GetOrCreate<T>(T prefab, int initialCount, bool autoExpand = true)
            where T : MonoBehaviour
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            _ = Root;

            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pools))
            {
                pools = new Dictionary<MonoBehaviour, WPoolBase>();
                _pools[type] = pools;
            }

            if (pools.TryGetValue(prefab, out var existing))
                return (WPool<T>)existing;

            var pool = WPool<T>.CreateInternal(prefab, initialCount);
            pool.AutoExpand = autoExpand;
            pools[prefab] = pool;
            return pool;
        }

        public static T GetFreeElement<T>(T prefab = null, bool activate = true)
            where T : MonoBehaviour
        {
            var pool = prefab != null ? GetOrCreate(prefab, 0) : GetFirstPool<T>();
            var obj = pool.GetFreeElement(activate);
            if (obj != null) _elementToPool[obj] = pool;
            return obj;
        }

        public static void Release<T>(T prefab, T element) where T : MonoBehaviour
        {
            if (prefab == null || element == null) return;
            GetOrCreate(prefab, 0).Release(element);
        }

        public static void Release<T>(T element) where T : MonoBehaviour
        {
            if (element == null) return;

            if (_elementToPool.TryGetValue(element, out var poolBase) && poolBase is WPool<T> typedPool)
            {
                typedPool.Release(element);
                return;
            }

            foreach (var pools in _pools.Values)
                foreach (var candidate in pools.Values)
                    if (candidate is WPool<T> p && p.IsLeased(element))
                    {
                        p.Release(element);
                        _elementToPool[element] = p;
                        return;
                    }
        }

        public static bool HasPool<T>(T prefab) where T : MonoBehaviour
        {
            if (prefab == null) return false;
            return _pools.TryGetValue(typeof(T), out var pools) && pools.ContainsKey(prefab);
        }

        public static WPool<T> GetPool<T>(T prefab) where T : MonoBehaviour
        {
            if (prefab == null) return null;
            return _pools.TryGetValue(typeof(T), out var pools)
                   && pools.TryGetValue(prefab, out var pool)
                ? (WPool<T>)pool
                : null;
        }

        public static void PruneAll()
        {
            foreach (var pools in _pools.Values)
                foreach (var pool in pools.Values)
                    pool.Prune();

            var toRemove = new List<MonoBehaviour>();
            foreach (var kv in _elementToPool)
                if (kv.Key == null)
                    toRemove.Add(kv.Key);
            foreach (var k in toRemove)
                _elementToPool.Remove(k);
        }

        public static void Clear<T>() where T : MonoBehaviour
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pools)) return;

            foreach (var pool in pools.Values)
                pool.Clear();

            var toRemove = new List<MonoBehaviour>();
            foreach (var kv in _elementToPool)
                if (pools.ContainsValue(kv.Value))
                    toRemove.Add(kv.Key);
            foreach (var k in toRemove)
                _elementToPool.Remove(k);

            pools.Clear();
            _pools.Remove(type);
        }

        public static void ClearAll()
        {
            foreach (var pools in _pools.Values)
                foreach (var pool in pools.Values)
                    pool.Clear();

            _pools.Clear();
            _elementToPool.Clear();
        }

        private static WPool<T> GetFirstPool<T>() where T : MonoBehaviour
        {
            var type = typeof(T);
            if (!_pools.TryGetValue(type, out var pools) || pools.Count == 0)
                throw new InvalidOperationException(
                    $"Пул для типа {type} не создан. Сначала вызовите WPool.GetOrCreate(...).");

            foreach (var pool in pools.Values)
                return (WPool<T>)pool;

            throw new InvalidOperationException();
        }
    }
}