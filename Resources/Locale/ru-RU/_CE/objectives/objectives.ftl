ce-lover-assist-objective-title = Помочь { $targetName }, должность: { CAPITALIZE($job) }, выполнить свои цели
ce-lover-survive-objective-title = Обеспечить, чтобы { $targetName }, должность: { CAPITALIZE($job) }, остался жив
ce-tormentor-spite-objective-title = Сделать так, чтобы { $targetName }, должность: { CAPITALIZE($job) }, провалил свои цели
ce-tormentor-survive-objective-title = Обеспечить, чтобы { $targetName }, должность: { CAPITALIZE($job) }, остался жив
ce-nightmare-hunt-objective-title = Убить { $targetName }, должность: { CAPITALIZE($job) }
ce-brotherhood-patronage-objective-title = Покровительство: { $targetName }, должность: { CAPITALIZE($job) }
ce-brotherhood-debt-objective-title = Должок: { $targetName }, должность: { CAPITALIZE($job) }
ce-recruiter-recruit-objective-title = Завербуйте {$count} {$count ->
    [one] человека
    *[other] человек
}

ce-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Выполнено[/color]
    *[false] [color=red]Провалено[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}

ce-objective-city-restore-desc = Купите у Каравана теней всё, что нужно для Ритуала восстановления, и проведите его у Сферы Люксона, пока мрак не поглотил город. Цены теней:
ce-objective-city-restore-desc-unknown = Тени ещё не назвали цену.
ce-objective-city-restore-price = - {$reward}: {$cost}
ce-objective-city-restore-reward-shard = {$index ->
    [1] Первый осколок светсердца
    [2] Второй осколок светсердца
    [3] Третий осколок светсердца
    [4] Четвёртый осколок светсердца
    *[other] Осколок светсердца №{$index}
}
ce-objective-city-restore-reward-book = Книга «Восстановление сферы»
