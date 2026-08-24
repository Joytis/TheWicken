# TODO — Staging / Inbox

- Bug (User Report): bottle barrage state divergence in Multiplayer 
- Relic Change: Tasty Herbs - 
- Bug: Call of the Pack: Text issue with upgrade.
- Visual: Brambles Power popup is way too big 
- Stony brew: Does not generate strictly defensive potions (Fertilizer)
- Torment: Do not the cantrip. (Rework)
- Cat familiar: nimble not upgrading?
- Card Change: Familiars - Instead of generating a random card, let's cycle *through* the cards. First turnk you make the first card in the card list. Second turn, you make the second turn in the cast list, then repeat. This would require tracking state for each power stack (which is a bit of a pain!), but may be worth exploring. 
- Crystal Bottle: Power tooltip - note excluded potions. 
- Card Change - Witchcraft: If you overflow your potion belt, created potions are automatically played. 
- Bag of Teeth: maybe uncommon
- Card Change: Owl Familiar (Knowledge): Cannot target cards with exhaust. Upgrade discount is this turn.
- Card Change: Separatory funnel: Moved to touch of orobas (Ancient) - (Remove the current ancient relic of 'Gain 3 potion slots)')
- Cozy nest: move to rare.

- Analytics: Currently, when building the analytics data for the dashboard, we ONLY pull the past 7 days. We should pull *all* of the analytics and process them. Additionally, there is a lapse on 8-21 and 8-22 - this seems like a bug. I'm seeing quite a few runs here on Supabase. Is there ove-eager filtering here? 
- New Multiplayer Card: Plague Tide: 2e - Rare - Power. ALL players summon a rat familiar. Upgrade: All players sommon a Rat Familiar+
- New Multiplayer Card: Blood Annointment: 1e - Skill - Uncommon. Choose a player. Their attacks apply 1 Hex this turn. Upgrade: -1e
- New Multiplayer Card: Bottle Bombardment: 2e - Attack - Rare. Deal 6 damage for each Potion created by ANY player this combat. {InCombat:\n(Hits {CalculatedHits:diff()} {CalculatedHits:plural:time|times})|}. Upgrade - +4 damage. 


# BENCHED - NEEDS FURTHER EVALUATION
- Need Unique VFX for Primal Form. 
- Need Unique VFX for Bottle Barrage. 