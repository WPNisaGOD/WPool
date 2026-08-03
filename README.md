# WPool – лёгкий объектный пул для Unity

`WPool<T>` — это универсальный пул для любых `MonoBehaviour`-компонентов. Он минимизирует вызовы `Instantiate`/`Destroy`, снижает нагрузку на сборщик мусора и подходит для частого спавна/деактивации объектов (враги, снаряды, эффекты и т.д.)

## Особенности

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
    private WPool<Bullet> _bulletPool;

    private void Awake()
    {
        // 20 предсозданных пуль, родитель – этот же объект
        _bulletPool = new WPool<Bullet>(bulletPrefab, 20, transform);
    }

    public void Shoot(Vector3 position, Quaternion rotation)
    {
        Bullet bullet = _bulletPool.GetFreeElement();
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.Init(/* параметры */);
    }
}
```
## От автора
Айо, это `WPNisaGOD`! Если вы вдруг используете мои пакеты, то будет мегакруто если вы где-нибудь мельком напишите `Респект WPNisaGOD`. Всем рад <3