ce-lover-assist-objective-title = Помочь { $targetName }, должность: { CAPITALIZE($job) }, выполнить свои цели
ce-lover-survive-objective-title = Обеспечить, чтобы { $targetName }, должность: { CAPITALIZE($job) }, остался жив
ce-nightmare-hunt-objective-title = Убить { $targetName }, должность: { CAPITALIZE($job) }

ce-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Выполнено[/color]
    *[false] [color=red]Провалено[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}
