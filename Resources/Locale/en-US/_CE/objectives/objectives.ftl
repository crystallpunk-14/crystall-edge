ce-lover-assist-objective-title = Help {$targetName}, {CAPITALIZE($job)} complete their objectives
ce-lover-survive-objective-title = Ensure {$targetName}, {CAPITALIZE($job)} stays alive
ce-tormentor-spite-objective-title = Make {$targetName}, {CAPITALIZE($job)} fail their objectives
ce-tormentor-survive-objective-title = Ensure {$targetName}, {CAPITALIZE($job)} stays alive
ce-nightmare-hunt-objective-title = Kill {$targetName}, {CAPITALIZE($job)}
ce-brotherhood-patronage-objective-title = Patronage: {$targetName}, {CAPITALIZE($job)}
ce-brotherhood-debt-objective-title = A debt: {$targetName}, {CAPITALIZE($job)}
ce-recruiter-recruit-objective-title = Recruit {$count} {$count ->
    [one] person
    *[other] people
}

ce-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Success[/color]
    *[false] [color=red]Failed[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}

ce-objective-city-restore-desc = Buy from the Shadow Caravan everything the Restoration Ritual needs, then perform it at the Lucson Sphere before the murk consumes the city. The shadows' prices:
ce-objective-city-restore-desc-unknown = The shadows haven't named their price yet.
ce-objective-city-restore-price = - {$reward}: {$cost}
ce-objective-city-restore-reward-shard = {$index ->
    [1] First lightheart shard
    [2] Second lightheart shard
    [3] Third lightheart shard
    *[other] Lightheart shard #{$index}
}
ce-objective-city-restore-reward-book = Restoration of the sphere book
