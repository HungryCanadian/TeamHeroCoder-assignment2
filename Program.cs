using System.IO;
using TeamHeroCoderLibrary;

namespace PlayerCoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting...");
            GameClientConnectionManager connectionManager;
            connectionManager = new GameClientConnectionManager();
            connectionManager.SetExchangePath(MyAI.FolderExchangePath);
            connectionManager.onHeroHasInitiative = MyAI.ProcessAI;
            connectionManager.StartListeningToGameClientForHeroPlayRequests();
        }
    }

    public static class MyAI
    {
        public static string FolderExchangePath = "C:/Users/davis/AppData/LocalLow/Wind Jester Games/Team Hero Coder";
        const int RESURRECTION_COST = 25;
        const int QUICK_HIT_COST = 15;
        const int CURE_SERIOUS_COST = 20;
        const int DEBRAVE_COST = 10;
        const int DEFAITH_COST = 10;
        const int FLURRY_OF_BLOWS_COST = 15;
        const int AUTO_LIFE_COST = 25;
        const int POISON_NOVA_COST = 15;
        const int MAGIC_MISSILE_COST = 10;
        const int METEOR_COST = 60;
        const int QUICK_CLEANSE_COST = 10;


        static public void ProcessAI()
        {
            Console.WriteLine("Processing AI!");
            Hero activeHero = null;
            bool hasPerformedAction = false;
            bool isSilenced = false;
            float useEtherAmount = 0.3f;
            bool hasEther = false;
            bool hasAutoLife = false;
            bool hasPoisonEffect = false;

            #region Code
            #region Fighter
            if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Fighter)
            {
                Console.WriteLine("this is a fighter");

                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;
                //The character with initiative is a figher, do something here...
                //What is the goal of a Fighter? IE What should a Fighter be doing?

                //HIGHEST PRIORITY CHECK GOES HERE
                //Ressurecting the Cleric if they are dead
                //Sucks to be a wizard; he's the cleric's job
                Console.WriteLine("CHECK: Ressurecting the Cleric if they are dead");
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if (ally.health <= 0)
                    {
                        Console.WriteLine("We found a dead ally");
                        if (ally.jobClass == HeroJobClass.Cleric)
                        {
                            Console.WriteLine("We found a dead cleric.");
                            if (activeHero.mana >= RESURRECTION_COST)
                            {
                                Console.WriteLine("We have the Mana for Ressurection. Casting it.");
                                //Placeholder calling the function note for us
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Resurrection, ally);
                            }
                        }
                    }
                }

                //Check to see if any ally needs an ether (35%/40% Mana remaining.)
                //Things we need to know before we can perform the "Use Ether" action
                //1: Do we have an ether availabe?
                Console.WriteLine("CHECK: See if any ally needs an ether (35%/40% Mana remaining.)");

                foreach (InventoryItem item in TeamHeroCoder.BattleState.allyInventory)
                {
                    if (item.item == Item.Ether)
                    {
                        Console.WriteLine("We still have Ethers");
                        hasEther = true;
                    }
                }

                //2: Which team member needs the ether?
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if ((float)ally.mana / (float)ally.maxMana <= 0.35 && hasEther)
                    {
                        Console.WriteLine("An ally is below 35% Mana. Using an Ether");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Ether, ally);
                    }
                }

                //Buff self with Brave if we (Fighter) don't have the Status
                //  Check to see if we have enough mana to still cast resurrection before casting brave
                //  Check to see what the Cleric's HP is before we commit to casting "buff" spells.
                Console.WriteLine("CHECK: Buff self with Brave if we (Fighter) don't have the Status");
                bool hasBrave = false;
                bool shouldCastBrave = false;

                foreach (StatusEffectAndDuration se in activeHero.statusEffectsAndDurations)
                {
                    if (se.statusEffect == StatusEffect.Brave)
                    {
                        Console.WriteLine("Fighter already has brave");
                        hasBrave = true;
                        break;
                    }

                    hasBrave = false;
                }

                if (activeHero.mana >= RESURRECTION_COST)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ally.jobClass == HeroJobClass.Cleric)
                        {
                            if ((float)ally.health / (float)ally.maxHealth >= 0.3)
                            {
                                Console.WriteLine("We are saving enough MP for emergency Res and Cleric is healthy. Brave is okay");
                                shouldCastBrave = true;
                            }
                        }
                    }
                }


                if (!hasBrave)
                {
                    Console.WriteLine("Fighter doesn't have brave");
                    if (shouldCastBrave)
                    {
                        Console.WriteLine("We have determined that casting brave is a good idea.");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Brave, activeHero);
                    }
                }

                //Check to see if any ally needs negative status effect removal.
                Console.Write("CHECK: See if any ally needs negative status effect removal.");
                bool haspoisonremedy = false;
                //MISSING:
                //Silence Rem Check
                //Petrify Rem Check
                //Full Rem Check

                foreach (InventoryItem item in TeamHeroCoder.BattleState.allyInventory)
                {
                    if (item.item == Item.PoisonRemedy)
                    {
                        Console.WriteLine("we still have poison remedies");
                        haspoisonremedy = true;
                    }
                }

                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    foreach (StatusEffectAndDuration se in ally.statusEffectsAndDurations)
                    {
                        if (se.statusEffect == StatusEffect.Poison && haspoisonremedy)
                        {
                            Console.WriteLine("Using Poison Rem");
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.PoisonRemedy, ally);
                        }
                    }
                }

                //Check if a party member is low HP AND if the cleric's turn is "far away". If so, Cure Serious
                //  Prioritize healing cleric over wizard or self
                Console.WriteLine("CHECK: Cure Serious if a party member is low HP and cleric's turn is far away.");

                if (activeHero.mana >= CURE_SERIOUS_COST)
                {
                    Hero cleric = null;
                    List<Hero> lowHP = new List<Hero>();
                    foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)hero.health / (float)hero.maxHealth <= 0.40)
                        {
                            lowHP.Add(hero);
                        }
                        if (hero.jobClass == HeroJobClass.Cleric)
                        {
                            cleric = hero;
                        }
                    }
                    foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (cleric.initiativePercent <= 50 && lowHP.Contains(cleric))
                        {
                            Console.WriteLine("Cleric's initiavePercent <= 50 and there is an ally below 40% Health. Casting Cure Serious");

                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, cleric);
                        }
                        else if (lowHP.Contains(hero))
                        {
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, hero);
                        }
                    }
                }


                //Look into checking to see if we can "one-shot" a damaged enemy.


                //Use quick hit if there are enemy alchemists/Rogues
                Console.WriteLine("CHECK: Use quick hit if there are enemy alchemists/Rogues");

                Hero quickHitTarget = null;
                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (hero.jobClass == HeroJobClass.Alchemist || hero.jobClass == HeroJobClass.Rogue)
                    {
                        if (quickHitTarget == null)
                            quickHitTarget = hero;

                        Console.WriteLine("We found a " + hero.jobClass + "in opposing team. Using Quick Hit");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickHit, hero);
                    }
                }

                if (activeHero.mana >= RESURRECTION_COST + QUICK_HIT_COST)
                {
                    Console.WriteLine("Fighter's total MP is greater than the cost of Res + Quick hit");
                    foreach (Hero h in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (h.jobClass == HeroJobClass.Alchemist || h.jobClass == HeroJobClass.Rogue)
                        {
                            Console.WriteLine("We found a " + h.jobClass + "in opposing team. Using Quick Hit");
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickHit, h);
                            return;
                        }
                    }
                }

                //  Check to see if we have enough mana to still cast resurrection before casting brave

                //Target Enemy with Lowest HP
                Console.WriteLine("CHECK: Attacking foe with lowest HP");
                Hero target = null;

                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (hero.health > 0)
                    {
                        if (target == null)
                            target = hero;
                        else if (hero.health < target.health)
                            target = hero;
                    }
                }

                //This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
                TeamHeroCoder.PerformHeroAbility(Ability.Attack, target);
                //LOWEST PRIORITY CHECK GOES HERE

            }
            #endregion Fighter
            #region Monk
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Monk)
            {

                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a Monk");
                Hero target = null;

                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("We are silenced Shhhh");
                    isSilenced = true;
                }


                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking for Brave!");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {


                        if (HasStatus(foe, StatusEffect.Brave) && activeHero.mana >= DEBRAVE_COST && !isSilenced)
                        {
                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target is Brave. Make them a coward!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Debrave, foe);
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking if they have Faith!");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (HasStatus(foe, StatusEffect.Faith) && activeHero.mana >= DEFAITH_COST && !isSilenced)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target has Faith. Casting Blasphemy");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Defaith, foe);
                            }
                        }

                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking if anyone is Poisoned");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (HasStatus(foe, StatusEffect.Poison) && activeHero.mana >= FLURRY_OF_BLOWS_COST && !isSilenced)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Target is Poisoned. Hit them where it hurts!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.FlurryOfBlows, foe);
                            }
                        }

                    }
                }


                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {

                    if (hero.health > 0)
                    {
                        if (target == null)
                            target = hero;
                        else if (hero.health < target.health)
                            target = hero;
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("No Statuses on Enemy team. Punch lowest health enemy in the face!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                }
            }
            #endregion Monk
            #region Cleric
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Cleric)
            {
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;
                int statusEffectCount = 0;

                //The character with initiative is a cleric, do something here...

                Console.WriteLine("this is a cleric");
                Hero target = null;

                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("we are silenced Shhhh");
                    isSilenced = true;
                }

                if (!hasPerformedAction)
                {
                    Console.WriteLine("Does anyone need healing?");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)ally.health / ally.maxHealth <= 0.3f)
                        {
                            Console.WriteLine("We found a wounded ally");
                            if (activeHero.mana >= CURE_SERIOUS_COST + RESURRECTION_COST && ally.health > 0 && !isSilenced)
                            {

                                if (!hasPerformedAction)
                                {
                                    Console.WriteLine("We have the Mana for Cure Serious with enough left over for Resurrection. Casting it.");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, ally);
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("We dont have enough mana to make sure we can cast resurrection, skipping the heal.");
                            }
                        }
                    }
                }
                Console.WriteLine("Do we have any Ethers?");
                if (HasItem(Item.Ether))
                {
                    Console.WriteLine("We still have Ethers");
                    hasEther = true;
                }

                if (!hasPerformedAction)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)ally.mana / (float)ally.maxMana <= useEtherAmount && hasEther)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("An ally is below 30% Mana. Force them to drink!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Ether, ally);
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Checking for Negative Status effects");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (GetNegativeStatusEffectCount(ally) >= 1)
                        {
                            if (activeHero.mana >= QUICK_CLEANSE_COST && !isSilenced)
                            {
                                if (!hasPerformedAction)
                                {
                                    Console.WriteLine("Target has Several Negative Ailments. Cleanse them!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickCleanse, ally);
                                }
                            }
                            else
                            {
                              Console.WriteLine("We don't have enough mana to cast QuickCleanse, or we are silenced.");
                            }
                        }
                    }
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("is Anyone dead?");
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ally.health <= 0)
                        {
                            Console.WriteLine("We found a dead ally");
                            if (activeHero.mana >= RESURRECTION_COST && !isSilenced)
                            {

                                if (!hasPerformedAction)
                                {
                                    Console.WriteLine("We have the Mana to revive someone! REVIVE THEM!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Resurrection, ally);
                                }
                                else
                                {
                                    Console.WriteLine("We dont have enough mana. skipping");
                                }
                            }
                        }
                    }
                    if (!hasPerformedAction)
                    {
                        foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                        {
                            Console.WriteLine("Checking for petrifying or petrified");
                            if (HasStatus(ally, StatusEffect.Petrifying) || HasStatus(ally, StatusEffect.Petrified))
                            {
                                Console.WriteLine("We found petrified or petrifying. They are Dirty! clean them!");
                                if (activeHero.mana >= QUICK_CLEANSE_COST && !isSilenced)
                                {
                                    if (!hasPerformedAction)
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.QuickCleanse, ally);
                                }
                                else if (activeHero.mana < QUICK_CLEANSE_COST || isSilenced)
                                {
                                    Console.WriteLine("We don't have enough mana to cast QuickCleanse, or we are silenced.");
                                }
                            }
                        } 
                    }
                    if (!hasPerformedAction)
                    {
                        Console.WriteLine("No Healing or Revive needed. AUTO LIFE EVERYTHING!");
                        foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                        {
                            hasAutoLife = false;
                            if (HasStatus(ally, StatusEffect.AutoLife))
                            {
                                Console.WriteLine("We found an ally with Auto Life, skipping this hero");
                                hasAutoLife = true;
                                continue;
                            }

                            if (activeHero.mana >= AUTO_LIFE_COST && !isSilenced && !hasAutoLife)
                            {

                                if (!hasPerformedAction)
                                {
                                    Console.WriteLine("Found someone without Autolife and We have the Mana for it!");
                                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.AutoLife, ally);
                                }
                            }
                            else
                            {
                                Console.WriteLine("We don't have enough mana to cast Auto Life, or we are silenced.");
                            }
                        }
                    }

                    foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (hero.health > 0)
                        {
                            if (target == null)
                                target = hero;
                            else if (hero.health < target.health)
                                target = hero;
                        }
                    }

                    if (!hasPerformedAction)
                    {
                        Console.WriteLine("No Healing or Revive needed. Everyone has Auto life.... ummmmm Hit the weakest person?");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                    }
                }
            }
            #endregion Cleric
            #region Wizard
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Wizard)
            {
                //The character with initiative is a wizard, do something here...
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a wizard");
                Hero target = null;


                if (HasStatus(activeHero, StatusEffect.Silence))
                {
                    Console.WriteLine("we are silenced Shhhh");
                    isSilenced = true;
                }


                Console.WriteLine("Do we have any Ethers?");
                if (HasItem(Item.Ether))
                {
                    Console.WriteLine("We still have Ethers");
                    hasEther = true;
                }

                if (!hasPerformedAction)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)ally.mana / (float)ally.maxMana <= useEtherAmount && hasEther)
                        {
                            Console.WriteLine("An ally is below 30% Mana. Using an Ether");
                            hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Ether, ally);

                        }
                    }
                }
                if (!hasPerformedAction && activeHero.mana >= POISON_NOVA_COST)
                {
                    Console.WriteLine("Wizard's total MP is greater than the cost of Poison Nova!");
                    Console.WriteLine("Checking for Posioned enemies");
                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {

                        if (HasStatus(foe, StatusEffect.Poison))
                        {
                            Console.WriteLine("We found a poisoned enemy");
                            hasPoisonEffect = true;
                            break;
                        }

                        if (!hasPoisonEffect && foe.health > 0 && !isSilenced)
                        {

                            if (!hasPerformedAction)
                            {
                                Console.WriteLine("Found someone who isn't poisoned and they are Alive");
                                Console.WriteLine("We have the Mana for Poison Nova!");
                                hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.PoisonNova, foe);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Not enough mana for Poison Nova!");
                }

                if (!hasPerformedAction)
                {
                    List<Hero> standingFoes = new List<Hero>();

                    foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (foe.health > 0)
                        {
                            standingFoes.Add(foe);
                        }
                    }

                    // Use Meteor if 2 or more foes are still standing
                    if (standingFoes.Count >= 1 && activeHero.mana >= METEOR_COST && !isSilenced)
                    {
                        Console.WriteLine("More than 2 foes standing, Make them regret it! METEOR!");
                        hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Meteor, target);
                        return;
                    }

                    foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (hero.health > 0)
                        {
                            if (target == null)
                                target = hero;
                            else if (hero.health < target.health)
                                target = hero;
                        }
                    }
                }

                if (!hasPerformedAction && activeHero.mana >= MAGIC_MISSILE_COST && !isSilenced)
                {
                    Console.WriteLine("Nothing to do! Targeting enemy with lowest HP");
                    Console.WriteLine("Magic Missile!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.MagicMissile, target);
                }
                if (!hasPerformedAction)
                {
                    Console.WriteLine("Nothing to do! Targeting enemy with lowest HP");
                    Console.WriteLine("out of Mana! hit them with your stick!");
                    hasPerformedAction = AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                }
            }
            #endregion Wizard
            #region samplecode
            foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
            {
                //How we look THROUGH our inventory
                if (ii.item == Item.Potion)
                {
                    //We found a potion
                }
            }


            //Searching for a poisoned hero 
            foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
            {
                foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
                {
                    if (se.statusEffect == StatusEffect.Poison)
                    {
                        //We have found a character that is poisoned, do something here...
                    }
                }
            }
            #endregion samplecode
            #endregion

        }

        #region Functions
        static public bool AttemptToPerformAction(bool hasPerformedAction, Ability ability, Hero target)
        {
            if (!hasPerformedAction)
            {
                TeamHeroCoder.PerformHeroAbility(ability, target);
                return true;
            }

            return false;
        }

        static public bool HasStatus(Hero hero, StatusEffect effect)
        {
            foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
            {
                if (se.statusEffect == effect) return true;
            }

            return false;
        }

        static public bool HasItem(Item item)
        {
            foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
            {
                if (ii.item == item && ii.count > 0) return true;
            }
            return false;
        }

        static public int GetNegativeStatusEffectCount(Hero hero)
        {
            int statusEffectCount = 0;
            foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
            {
                if (se.statusEffect == StatusEffect.Defaith) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Debrave) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Doom) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Petrifying) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Petrified) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Silence) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Poison) statusEffectCount++;
                if (se.statusEffect == StatusEffect.Slow) statusEffectCount++;
            }
            return statusEffectCount;
        }
        #endregion Functions
    }
}