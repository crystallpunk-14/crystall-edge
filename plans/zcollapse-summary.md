# ZCollapse — обрушение тайлов

Ядро заливает стабильность по тайлам (-1/шаг), Опоры мостят её между текущим и верхним Z-уровнем. Тайл на 0 удаляется, сущности на нём — через DestroyEntity.

Проблемы и решения:
- RegisterComponent требует суффикс "Component" — переименовали классы.
- Карта рушилась при инициализации: MapInitEvent идёт раньше детей-сущностей — добавили отложенный пересчёт после полной загрузки.
- Снос одного из двух Ядер стирал почти всё поле: Propagate не перетолкал сидовые тайлы с неизменным значением — исправили релайт.
- Опоры создавали стабильность из воздуха, сидя SupportStrength всегда, когда сторона жива — заменили на min(strength, реальное значение источника).
- Один словарь BridgeSeeds писали два разных моста (опора здесь и опора снизу) — гонка перезаписи. Разделили на FromAbove/FromBelow.
- При LevitationForce 50 сервер зависал: сотни сущностей при загрузке карты каждая делала свой BFS. Перешли на пакет: сид Ядер → один устоявшийся проход мостов → одна чистка мёртвых тайлов.

## Структура решения

Shared/_CE/ZCollapse/Events — сетевые DTO оверлея (алгоритм только на сервере).

Server/_CE/ZCollapse/:
- CEGridStabilityCoreComponent, CEGridStabilitySupportComponent — LevitationForce, SupportStrength.
- CEGridStabilityComponent — opt-in маркер грида, хранит Stability и раздельные сиды (Core/BridgeFromAbove/BridgeFromBelow).
- CEZCollapseSystem.cs — главный цикл: отложенный сид Ядер после загрузки карты → устоявшийся проход мостов → чистка мёртвых тайлов.
- .Core.cs / .Support.cs — реакция на анкер/переанкер, RecomputeBridge для мостов.
- .Propagate.cs / .Depropagate.cs — инкрементальный разлив/погашение стабильности (BFS, как свет в Minecraft).
- .TileEvents.cs — внешние изменения тайлов (RCD, взрывы).
- .Destruction.cs — уничтожение сущностей на упавшем тайле через SharedDestructibleSystem.
- .Debug.cs — снапшот стабильности по подписанным сессиям (по образцу RadiationSystem).
- .Recalc.cs — полный пересчёт одного грида, фолбэк для админов.
- Commands/ — showgridstability, znetwork-collapserecalc.

Client/_CE/ZCollapse/ — приём снапшота с сервера + отрисовка оверлея (заливка бело-красная, текст со значением).

Прототипы: Ядро — Entities/Structures/Industrial/core.yml; Опора добавлена всем стенам через CEBaseWall; opt-in грида — zLevelsComponentOverrides в Maps/debug.yml.
