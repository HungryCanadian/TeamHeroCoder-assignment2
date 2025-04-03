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

        static bool hasPerformedAction = false;


        static public void ProcessAI()
        {
            Console.WriteLine("Processing AI!");
            Hero activeHero = null;

            #region SampleCode

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
                                AttemptToPerformAction(Ability.Resurrection, ally);
                            }
                        }
                    }
                }

                //Check to see if any ally needs an ether (35%/40% Mana remaining.)
                //Things we need to know before we can perform the "Use Ether" action
                //1: Do we have an ether availabe?
                Console.WriteLine("CHECK: See if any ally needs an ether (35%/40% Mana remaining.)");
                bool hasEther = false;

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
                        TeamHeroCoder.PerformHeroAbility(Ability.Ether, ally);
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
                        TeamHeroCoder.PerformHeroAbility(Ability.Brave, activeHero);
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
                            TeamHeroCoder.PerformHeroAbility(Ability.PoisonRemedy, ally);
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

                            TeamHeroCoder.PerformHeroAbility(Ability.CureSerious, cleric);
                        }
                        else if (lowHP.Contains(hero))
                        {
                            TeamHeroCoder.PerformHeroAbility(Ability.CureSerious, hero);
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
                        TeamHeroCoder.PerformHeroAbility(Ability.QuickHit, hero);
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
                            TeamHeroCoder.PerformHeroAbility(Ability.QuickHit, h);
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
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Monk)
            {
                //The character with initiative is a cleric, do something here...
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a Monk");
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
                TeamHeroCoder.PerformHeroAbility(Ability.FlurryOfBlows, target);
            }
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Cleric)
            {
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;
                //The character with initiative is a cleric, do something here...

                Console.WriteLine("this is a cleric");
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
            }
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Wizard)
            {
                //The character with initiative is a wizard, do something here...
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;

                Console.WriteLine("this is a wizard");
                Hero target = null;
                int poisonNovaCost = 15;

                if (activeHero.mana >= poisonNovaCost)
                {
                    Console.WriteLine("Wizard's total MP is greater than the cost of Poison Nova!");
                    foreach (Hero h in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        foreach (StatusEffectAndDuration se in h.statusEffectsAndDurations)
                        {
                            if (se.statusEffect != StatusEffect.Poison)
                            {
                                Console.WriteLine("Target is not Poisoned. Casting Poison Nova");
                                AttemptToPerformAction(Ability.PoisonNova, h);
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

                //This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
                TeamHeroCoder.PerformHeroAbility(Ability.Attack, target);
            }


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

            #endregion

        }

        static void AttemptToPerformAction(Ability ability, Hero target)
        {
            if (!hasPerformedAction)
            {
                TeamHeroCoder.PerformHeroAbility(ability, target);
                hasPerformedAction = true;
            }
        }
    }
}