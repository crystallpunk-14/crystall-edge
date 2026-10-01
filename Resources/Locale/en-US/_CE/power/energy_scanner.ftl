ce-energy-scanner-network-big = [bold]Big pipe network[/bold]
ce-energy-scanner-network-medium = [bold]Medium pipe network[/bold]
ce-energy-scanner-network-other = [bold]Energy network[/bold]

ce-energy-scanner-powered = Status: [color=#7FE0FF]energized[/color]
ce-energy-scanner-unpowered = Status: [color=gray]no mana[/color]

ce-energy-scanner-statistics =
    {$header}
    {$status}
    Mana influx: {$supplyc} (accumulators: {$supplyb}, max: {$supplym})
    Total draw: {$consumption}
    Accumulators discharging: {$storagec} / {$storagem} ({ TOSTRING($storager, "P1") })
    Accumulators charging: {$storageoc} / {$storageom} ({ TOSTRING($storageor, "P1") })
