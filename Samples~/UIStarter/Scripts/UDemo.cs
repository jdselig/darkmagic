using UnityEngine;
using DarkMagic;

public class UDemo : MonoBehaviour
{
    private int _score = 0;
    private U.IDisplayHandle _hud;

    private async void Start()
    {
        _hud = U.Display(() => $"SCORE: {_score}", placement: U.Placements.TopLeft);

        await U.PopBanner("A party of goblins attacks!", placement: U.Placements.TopCenter);

        await U.PopDialogue("Wow, I never see people come all the way out here. You guys are nuts! If you make it back alive, come see me and I'll give you a special treasure—you won't want to miss it!");

        var choice = await U.PopChoice("Do you accept this quest?", "Yes", "Nope");

        if (!choice.Cancelled && choice.Value == "Yes")
        {
            _score += 5;
            await U.PopBanner("Quest accepted! (+5)", placement: U.Placements.TopCenter);
        }
        else
        {
            await U.PopBanner("Maybe later.", placement: U.Placements.TopCenter);
        }

        while (true)
        {
            var cmd = await U.Menu("Knight", "Fight", "Magic", "Defend", "Item");

            if (cmd.Cancelled) break;

            if (cmd.Value == "Fight")
            {
                await U.PopBanner("Choose a target (v1: UI-only choice).", placement: U.Placements.TopCenter);
                var t = await U.PopChoice("Target", "Goblin A", "Goblin B", "Back");
                if (t.Cancelled || t.Value == "Back") continue;
                await U.PopBanner($"You attack {t.Value}!", placement: U.Placements.TopCenter);
            }
            else if (cmd.Value == "Magic")
            {
                var spell = await U.PopChoice("Magic", "Firewave (all)", "Spark (single)", "Back");
                if (spell.Cancelled || spell.Value == "Back") continue;
                await U.PopBanner($"Cast {spell.Value}!", placement: U.Placements.TopCenter);
            }
            else
            {
                await U.PopBanner($"{cmd.Value}!", placement: U.Placements.TopCenter);
            }
        }
    }

    private void Update()
    {
        if (I.GetKeyDown(KeyCode.P))
            _score++;
    }

    private void OnDestroy()
    {
        _hud?.Dispose();
    }
}