pve-preset-title = PvE: Expedition
pve-preset-description = You start on a shuttle. Prepare as long as you need, then fly to the planet through the shuttle console. Tasks await in the dungeon, enemies roam outside. Complete every task, return the shuttle, and the shift ends.

pve-announcement-sender = Expedition Command
pve-destination-name = Landing beacon: uncharted planet
pve-planet-name = Uncharted planet

pve-start-announcement = The expedition is assembled. Preparation time is unlimited. Your task list is on the paper in your backpack. When the planet is ready, pick the landing beacon in the shuttle console and jump.
pve-planet-ready-announcement = Planet scan complete: a huge complex was found on the surface ({ $rooms } rooms). The landing beacon is available in the shuttle console.
pve-landed-announcement = The shuttle has landed. All targets are inside the complex. Expedition tasks:
pve-generation-failed = Failed to prepare the planet. The expedition is cancelled.

pve-task-optional = (optional)
pve-task-completed = Task completed: { $task }.
pve-ftl-blocked = Departure blocked: complete all required expedition tasks first.

pve-countdown-start = All tasks complete and the shuttle is back. { $seconds } seconds until the end of the shift.
pve-countdown-60 = 1 minute until the end of the shift.
pve-countdown-10 = 10 seconds until the end of the shift.
pve-round-end-text = The expedition is over.

pve-roundend-success = [color=lightgreen]The expedition completed every task and returned on the shuttle![/color]
pve-roundend-incomplete = [color=red]The expedition failed to complete its tasks.[/color]
pve-roundend-task-done = [color=lightgreen]✔[/color] { $name }
pve-roundend-task-failed = [color=red]✘[/color] { $name }

pve-paper-name = Expedition tasks
pve-paper-header = [bold][italic]EXPEDITION TASKS[/italic][/bold]
pve-paper-footer = All targets are inside the complex on the planet. You cannot leave the planet until every required task is done; then return the shuttle to where you started.

pve-task-kill-carp = Kill space carp: { $count }
pve-task-kill-carp-desc = A carp pack has nested in the complex.
pve-task-kill-spiders = Kill giant spiders: { $count }
pve-task-kill-spiders-desc = Spiders have taken over parts of the complex.
pve-task-kill-xenos = Kill xenomorphs: { $count }
pve-task-kill-xenos-desc = A xenomorph colony was detected in the complex.
pve-task-kill-bears = Kill space bears: { $count }
pve-task-kill-bears-desc = Large predators are blocking the corridors.
pve-task-collect-artifacts = Deliver artifacts to the shuttle: { $count }
pve-task-collect-artifacts-desc = Find the artifacts in the complex and load them onto the shuttle.
pve-task-collect-gold = Deliver gold ore to the shuttle: { $count }
pve-task-collect-gold-desc = Ore samples for the lab.
pve-task-destroy-generators = Destroy generators: { $count }
pve-task-destroy-generators-desc = Cut the complex's power. The pinpointer in your backpack points to the nearest generator.
pve-task-destroy-smes = Destroy SMES units: { $count }
pve-task-destroy-smes-desc = Wreck the energy storage. The pinpointer in your backpack points to the nearest one.
pve-task-return-shuttle = Return the shuttle to the starting point
pve-task-return-shuttle-desc = Complete all other required tasks, then fly the shuttle back to where you departed.

ent-PinpointerPveStructures = expedition pinpointer
    .desc = A handheld tracker locked onto the structures the expedition must destroy. Keep it on and follow the arrow.
