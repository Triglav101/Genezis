rmc-bioscan-ares-announcement = [color=white][font size=16][bold]Суперкомпьютер: результат биосканирования[/bold][/font][/color][color=red][font size=14][bold]
    {$message}[/bold][/font][/color]

rmc-bioscan-ares = Биосканирование завершено.

  Датчики показывают { $shipUncontained ->
    [0] нет
    *[other] {$shipUncontained}
  } неизвестная форма жизни { $shipUncontained ->
    [0] сигнатур
    [1] сигнатура
    *[other] сигнатур
  } на корабле{ $shipLocation ->
    [none] {""}
    *[other], в том числе на {$shipLocation},
  } и { $onPlanet ->
    [0] нет
    *[other] примерно {$onPlanet}
  } { $onPlanet ->
    [0] сигнатур
    [1] сигнатура
    *[other] сигнатур
  } расположенный где-то на { $planetLocation ->
    [none].
    *[other], в том числе на {$planetLocation}
  }

rmc-bioscan-xeno-announcement = [color=#318850][font size=14][bold]Разум планеты проникает в ваше сознание.
  {$message}[/bold][/font][/color]

rmc-bioscan-xeno = Всем моим рабам, что слышат мой глас { $onShip ->
  [0] нет вторженцев
  [1] примерно 1 вторженец
  *[other] примерно {$onShip}
} на корабле{ $shipLocation ->
  [none] {""}
  *[other], в том числе на {$shipLocation},
} и {$onPlanet ->
  [0] нет
  *[other] {$onPlanet}
} разбросанные где-то на {$planetLocation ->
  [none].
  *[other], в том числе на {$planetLocation}
}
