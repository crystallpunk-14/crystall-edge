ce-energy-scanner-network-big = [bold]Сеть больших труб[/bold]
ce-energy-scanner-network-medium = [bold]Сеть средних труб[/bold]
ce-energy-scanner-network-other = [bold]Энергосеть[/bold]

ce-energy-scanner-powered = Состояние: [color=#7FE0FF]под напряжением[/color]
ce-energy-scanner-unpowered = Состояние: [color=gray]нет маны[/color]

ce-energy-scanner-statistics =
    {$header}
    {$status}
    Приток маны: {$supplyc} (из аккумуляторов: {$supplyb}, максимум: {$supplym})
    Общее потребление: {$consumption}
    Разрядка аккумуляторов: {$storagec} / {$storagem} ({ TOSTRING($storager, "P1") })
    Зарядка аккумуляторов: {$storageoc} / {$storageom} ({ TOSTRING($storageor, "P1") })
