# WPool – лёгкий объектный пул для Unity

`WPool<T>` — это универсальный пул для любых `MonoBehaviour`-компонентов. Он минимизирует вызовы `Instantiate`/`Destroy`, снижает нагрузку на сборщик мусора и подходит для частого спавна/деактивации объектов (враги, снаряды, эффекты и т.д.)

## Особенности

- **Единый статический доступ** – `WPool.GetFreeElement(...)` и `WPool.Release(...)` вызываются из любого места без ссылок на пул
- **Все пулы в одном месте** – один статический класс хранит пулы всех типов и всех префабов
- **Несколько префабов одного типа** – каждый префаб имеет свой независимый пул
- **Release без указания префаба** – менеджер сам находит пул через reverse-словарь
- **Защита от двойного возврата** – `HashSet<T>` _leased не даст вернуть один и тот же объект дважды
- **Быстрое получение** – стековая структура гарантирует O(1) при выдаче и возврате
- **AutoExpand** – пул может автоматически создавать новые объекты, если свободных не осталось
- **Предварительный прогрев** – задайте начальное количество экземпляров в конструкторе
- **Возврат в пул** – метод `Release()` деактивирует объект и помещает его обратно
- **Гибкая активация** – можно получить объект без немедленного включения (`activate: false`), чтобы настроить его до появления на сцене
- **Родительский контейнер** – все объекты создаются внутри указанного `Transform` (опционально)

## Установка

1. Откройте Unity, перейдите в `Window → Package Manager`.
2. Нажмите `+`, выберите `Add package from git URL…`.
3. Вставьте ссылку на репозиторий: https://github.com/WPNisaGOD/WPool.git
4. Нажмите `Add` и дождитесь завершения импорта.

## Быстрый старт

```csharp
using WPNisaGOD.WPool;

public class Gun : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;

    private void Awake()
    {
        WPool.GetOrCreate(bulletPrefab, 20, container: transform);
    }

    public void Shoot(Vector3 position, Quaternion rotation)
    {
        Bullet bullet = WPool.GetFreeElement(bulletPrefab);
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.Init(/* параметры */);
    }
}

public class Bullet : MonoBehaviour
{
    private void OnDisable()
    {
        WPool.Release(this); // пул найдётся автоматически
    }
}
```

## API

| Метод | Назначение|
|:-|:-|
|`WPool.GetOrCreate<T>(prefab, count, container, autoExpand)`|Создать пул или вернуть существующий|
|`WPool.GetFreeElement<T>(prefab = null, activate = true)`|Взять свободный объект|
|`WPool.Release<T>(element)`|Вернуть объект, префаб находится автоматически|
|`WPool.Release<T>(prefab, element)`|Вернуть в конкретный пул|
|`WPool.HasPool<T>(prefab)`|Проверить существование пула|
|`WPool.GetPool<T>(prefab)`|Получить `WPool<T>` напрямую|
|`WPool.Clear<T>()`|Уничтожить все пулы одного типа|
|`WPool.ClearAll()`|Уничтожить все пулы|

## От автора
Айо, это `WPNisaGOD`! Если вы вдруг используете мои пакеты, то будет мегакруто если вы где-нибудь мельком напишите `Респект WPNisaGOD`. Всем рад <3