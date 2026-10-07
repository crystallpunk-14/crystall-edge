ce-lover-assist-objective-title = Help {$targetName}, {CAPITALIZE($job)} complete their objectives
ce-lover-survive-objective-title = Ensure {$targetName}, {CAPITALIZE($job)} stays alive
ce-tormentor-spite-objective-title = Make {$targetName}, {CAPITALIZE($job)} fail their objectives
ce-tormentor-survive-objective-title = Ensure {$targetName}, {CAPITALIZE($job)} stays alive
ce-nightmare-hunt-objective-title = Kill {$targetName}, {CAPITALIZE($job)}
ce-brotherhood-patronage-objective-title = Patronage: {$targetName}, {CAPITALIZE($job)}
ce-brotherhood-debt-objective-title = A debt: {$targetName}, {CAPITALIZE($job)}

ce-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Success[/color]
    *[false] [color=red]Failed[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}
