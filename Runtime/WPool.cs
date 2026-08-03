using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WPNisaGOD.WPool
{
    public class WPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _container;
        private readonly Stack<T> _available = new Stack<T>();
        private readonly List<T> _allObjects = new List<T>();

        public bool AutoExpand { get; set; } = true;

        public WPool(T prefab, int initialCount) : this(prefab, initialCount, null) { }

        public WPool(T prefab, int initialCount, Transform container)
        {
            _prefab = prefab;
            _container = container;
            CreatePool(initialCount);
        }

        private void CreatePool(int count)
        {
            for (int i = 0; i < count; i++)
                CreateObject();
        }

        private T CreateObject(bool setActive = false)
        {
            T obj = Object.Instantiate(_prefab, _container);
            obj.gameObject.SetActive(setActive);
            _allObjects.Add(obj);
            if (!setActive)
                _available.Push(obj);
            return obj;
        }

        /// <summary>
        /// Возвращает свободный объект из пула. По умолчанию активирует его.
        /// </summary>
        /// <param name="activate">Активировать ли объект сразу.</param>
        public T GetFreeElement(bool activate = true)
        {
            if (_available.Count > 0)
            {
                T obj = _available.Pop();
                if (activate)
                    obj.gameObject.SetActive(true);
                return obj;
            }

            if (AutoExpand)
                return CreateObject(activate);

            throw new System.Exception(
                $"Нет свободных объектов в пуле типа {typeof(T)}. Включите AutoExpand.");
        }

        /// <summary>
        /// Возвращает объект обратно в пул (деактивирует его).
        /// </summary>
        public void Release(T element)
        {
            if (element == null) return;
            element.gameObject.SetActive(false);
            _available.Push(element);
        }

        /// <summary>
        /// Увеличивает ёмкость пула на указанное число объектов.
        /// </summary>
        public void Expand(int additionalCount)
        {
            for (int i = 0; i < additionalCount; i++)
                CreateObject();
        }

        /// <summary>
        /// Очищает пул, уничтожая все управляемые объекты.
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _allObjects)
            {
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
            _allObjects.Clear();
            _available.Clear();
        }
    }
}